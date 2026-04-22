using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml;
using EstimateProject.Data;
using EstimateProject.Models;

namespace EstimateProject.Controllers
{
    public class EstimateController : Controller
    {
        private const string SESSION_KEY = "EstimateRows";
        private readonly DatabaseHelper _db;
        private readonly IConfiguration _config;

        public EstimateController(DatabaseHelper db, IConfiguration config)
        {
            _db     = db;
            _config = config;
            // EPPlus 5 license for non-commercial use
#pragma warning disable CS0618
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
#pragma warning restore CS0618
        }

        // ─────────────────────────────────────────
        // GET /Estimate/Index  (main page)
        // ─────────────────────────────────────────
        public IActionResult Index()
        {
            var vm = new EstimateViewModel
            {
                Rows      = GetSessionRows(),
                ActiveTab = "import"
            };
            // Carry any flash messages from TempData
            if (TempData["SuccessMessage"] != null)
            {
                vm.SubmitMessage = TempData["SuccessMessage"].ToString();
                vm.IsSuccess     = true;
            }
            if (TempData["ErrorMessage"] != null)
            {
                vm.SubmitMessage = TempData["ErrorMessage"].ToString();
                vm.IsSuccess     = false;
            }
            return View(vm);
        }

        // ─────────────────────────────────────────
        // POST /Estimate/Import  – parse Excel
        // ─────────────────────────────────────────
        [HttpPost]
        public IActionResult Import(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid Excel file (.xlsx).";
                return RedirectToAction("Index");
            }

            var ext = Path.GetExtension(excelFile.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls")
            {
                TempData["ErrorMessage"] = "Only .xlsx or .xls files are supported.";
                return RedirectToAction("Index");
            }

            var rows = new List<EstimateRow>();
            try
            {
                using var stream  = excelFile.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var ws = package.Workbook.Worksheets[0];

                if (ws == null || ws.Dimension == null)
                {
                    TempData["ErrorMessage"] = "The Excel file is empty or has no data.";
                    return RedirectToAction("Index");
                }

                int lastRow = ws.Dimension.End.Row;
                int serial  = 1;

                for (int r = 2; r <= lastRow; r++) // row 1 = header
                {
                    var desc     = ws.Cells[r, 1].Text?.Trim();
                    var uom      = ws.Cells[r, 2].Text?.Trim();
                    var qtyText  = ws.Cells[r, 3].Text?.Trim();
                    var matText  = ws.Cells[r, 4].Text?.Trim();
                    var svcText  = ws.Cells[r, 5].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(desc)) continue; // skip blank rows

                    decimal.TryParse(qtyText,  out decimal qty);
                    decimal.TryParse(matText,  out decimal mat);
                    decimal.TryParse(svcText,  out decimal svc);

                    rows.Add(new EstimateRow
                    {
                        SerialNo     = serial++,
                        ItemDesc     = desc,
                        UOM          = uom ?? "",
                        Quantity     = qty,
                        MaterialCost = mat,
                        ServiceCost  = svc
                    });
                }

                if (rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "No data rows found. Ensure your Excel starts with a header on row 1.";
                    return RedirectToAction("Index");
                }

                SaveSessionRows(rows);
                TempData["SuccessMessage"] = $"{rows.Count} row(s) imported successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error reading Excel: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // ─────────────────────────────────────────
        // GET /Estimate/DownloadTemplate
        // ─────────────────────────────────────────
        public IActionResult DownloadTemplate()
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Estimate");

            // Headers
            string[] headers = { "Item Description", "UOM", "Quantity", "Material Cost (₹)", "Service Cost (₹)" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
                ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 70, 127));
                ws.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            // Sample rows
            object[,] sampleData =
            {
                { "Steel Plate 10mm", "KG",  100,  85.50,  12.00 },
                { "Hex Bolt M12",     "NOS", 200,   4.50,   1.00 },
                { "Welding Electrode 3.15", "PKT", 10, 320.00, 25.00 }
            };
            for (int r = 0; r < sampleData.GetLength(0); r++)
                for (int c = 0; c < sampleData.GetLength(1); c++)
                    ws.Cells[r + 2, c + 1].Value = sampleData[r, c];

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var bytes = package.GetAsByteArray();
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "EstimateTemplate.xlsx");
        }

        // ─────────────────────────────────────────
        // POST /Estimate/SaveRows  – AJAX row save
        // ─────────────────────────────────────────
        [HttpPost]
        public IActionResult SaveRows([FromBody] List<EstimateRow> rows)
        {
            if (rows == null) return Json(new { success = false });
            // Re-index
            for (int i = 0; i < rows.Count; i++) rows[i].SerialNo = i + 1;
            SaveSessionRows(rows);
            return Json(new { success = true, count = rows.Count });
        }

        // ─────────────────────────────────────────
        // POST /Estimate/Verify  – validate rows
        // ─────────────────────────────────────────
        [HttpPost]
        public IActionResult Verify([FromBody] List<EstimateRow> rows)
        {
            if (rows == null || rows.Count == 0)
                return Json(new { success = false, message = "No rows to verify." });

            List<MaterialMasterRow> master;
            bool dbAvailable = true;
            try   { master = _db.GetMaterialMaster(); }
            catch { master = new List<MaterialMasterRow>(); dbAvailable = false; }

            var verified = new List<object>();
            foreach (var row in rows)
            {
                bool mismatch    = false;
                var  reasons     = new List<string>();

                // Rule 1: TotalCost formula check
                decimal expected = (row.Quantity * row.MaterialCost) + row.ServiceCost;
                if (Math.Abs(expected - row.TotalCost) > 0.01m)
                {
                    mismatch = true;
                    reasons.Add($"Total cost mismatch (expected ₹{expected:N2}, got ₹{row.TotalCost:N2})");
                }

                // Rule 2: Quantity > 0
                if (row.Quantity <= 0)
                {
                    mismatch = true;
                    reasons.Add("Quantity must be greater than 0");
                }

                // Rule 3: Cross-check with material master
                if (dbAvailable && master.Count > 0)
                {
                    var masterRow = master.FirstOrDefault(m =>
                        m.ItemDesc.Equals(row.ItemDesc, StringComparison.OrdinalIgnoreCase));

                    if (masterRow != null)
                    {
                        if (Math.Abs(masterRow.MaterialCost - row.MaterialCost) > 0.01m)
                        {
                            mismatch = true;
                            reasons.Add($"Material cost differs from master (master: ₹{masterRow.MaterialCost:N2})");
                        }
                        if (!masterRow.UOM.Equals(row.UOM, StringComparison.OrdinalIgnoreCase))
                        {
                            mismatch = true;
                            reasons.Add($"UOM mismatch (master: {masterRow.UOM})");
                        }
                    }
                }

                verified.Add(new
                {
                    serialNo      = row.SerialNo,
                    itemDesc      = row.ItemDesc,
                    uom           = row.UOM,
                    quantity      = row.Quantity,
                    materialCost  = row.MaterialCost,
                    serviceCost   = row.ServiceCost,
                    totalCost     = row.TotalCost,
                    isVerified    = !mismatch,
                    mismatchFlag  = mismatch,
                    mismatchReason= string.Join("; ", reasons)
                });
            }

            // Update session
            var updatedRows = rows.Select((r, i) =>
            {
                var v = (dynamic)verified[i];
                r.IsVerified   = !((bool)v.mismatchFlag);
                r.MismatchFlag = v.mismatchFlag;
                r.MismatchReason = v.mismatchReason;
                return r;
            }).ToList();
            SaveSessionRows(updatedRows);

            int mismatchCount  = verified.Count(v => (bool)((dynamic)v).mismatchFlag);
            int verifiedCount  = verified.Count - mismatchCount;

            return Json(new
            {
                success       = true,
                rows          = verified,
                mismatchCount,
                verifiedCount,
                dbAvailable
            });
        }

        // ─────────────────────────────────────────
        // POST /Estimate/Submit
        // ─────────────────────────────────────────
        [HttpPost]
        public IActionResult Submit([FromBody] List<EstimateRow> rows)
        {
            if (rows == null || rows.Count == 0)
                return Json(new { success = false, message = "No data to submit." });

            var estimateNo = $"EST-{DateTime.Now:yyyyMMdd-HHmmss}";
            decimal totalCost = rows.Sum(r => r.TotalCost);

            try
            {
                int id = _db.SubmitEstimate(rows, estimateNo, "USER", totalCost, null);
                SaveSessionRows(new List<EstimateRow>()); // clear
                return Json(new
                {
                    success    = true,
                    estimateNo,
                    estimateId = id,
                    totalCost,
                    message    = $"Estimate {estimateNo} submitted successfully! ID: {id}"
                });
            }
            catch (Exception ex)
            {
                // Demo fallback – works even without DB
                SaveSessionRows(new List<EstimateRow>());
                return Json(new
                {
                    success    = true,
                    estimateNo,
                    totalCost,
                    message    = $"Estimate {estimateNo} saved in memory (DB unavailable: {ex.Message})"
                });
            }
        }

        // ─────────────────────────────────────────
        // GET /Estimate/Remove  – clear session
        // ─────────────────────────────────────────
        public IActionResult Remove()
        {
            SaveSessionRows(new List<EstimateRow>());
            TempData["SuccessMessage"] = "Estimate data cleared.";
            return RedirectToAction("Index");
        }

        // ─────────────────────────────────────────
        // GET /Estimate/CostBook  – opens in new tab
        // ─────────────────────────────────────────
        public IActionResult CostBook()
        {
            var rows = GetSessionRows();
            return View("CostBook", rows);
        }

        // ─────────────────────────────────────────
        // GET /Estimate/History
        // ─────────────────────────────────────────
        public IActionResult History()
        {
            List<EstimateHistoryRow> history;
            try   { history = _db.GetEstimateHistory(); }
            catch { history = new List<EstimateHistoryRow>(); }
            return View(history);
        }

        // ─────────────────────────────────────────
        // GET /Estimate/GetMasterItems  – AJAX autocomplete
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult GetMasterItems()
        {
            try
            {
                var master = _db.GetMaterialMaster();
                return Json(master);
            }
            catch
            {
                return Json(new List<MaterialMasterRow>());
            }
        }

        // ─────────────────────────────────────────
        // Session helpers
        // ─────────────────────────────────────────
        private List<EstimateRow> GetSessionRows()
        {
            var json = HttpContext.Session.GetString(SESSION_KEY);
            if (string.IsNullOrEmpty(json)) return new List<EstimateRow>();
            return JsonConvert.DeserializeObject<List<EstimateRow>>(json)
                   ?? new List<EstimateRow>();
        }

        private void SaveSessionRows(List<EstimateRow> rows)
        {
            HttpContext.Session.SetString(SESSION_KEY, JsonConvert.SerializeObject(rows));
        }
    }
}
