using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EstimateProject.Models
{
    // ─────────────────────────────────────────────
    // Core estimate row (used in grid + TempData)
    // ─────────────────────────────────────────────
    public class EstimateRow
    {
        public int SerialNo { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string ItemDesc { get; set; }

        [Required(ErrorMessage = "UOM is required")]
        public string UOM { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be > 0")]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Material cost must be ≥ 0")]
        public decimal MaterialCost { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Service cost must be ≥ 0")]
        public decimal ServiceCost { get; set; }

        // Computed: (Qty × MaterialCost) + ServiceCost
        public decimal TotalCost => (Quantity * MaterialCost) + ServiceCost;

        // Verification state
        public bool IsVerified    { get; set; }
        public bool MismatchFlag  { get; set; }
        public string MismatchReason { get; set; }
    }

    // ─────────────────────────────────────────────
    // Main view model passed to Index view
    // ─────────────────────────────────────────────
    public class EstimateViewModel
    {
        public List<EstimateRow> Rows { get; set; } = new List<EstimateRow>();
        public bool HasData => Rows != null && Rows.Count > 0;
        public decimal GrandTotal => Rows?.Sum(r => r.TotalCost) ?? 0;
        public int MismatchCount => Rows?.Count(r => r.MismatchFlag) ?? 0;
        public int VerifiedCount => Rows?.Count(r => r.IsVerified) ?? 0;
        public string SubmitMessage { get; set; }
        public bool IsSuccess { get; set; }
        public string ActiveTab { get; set; } = "import"; // import | verify
    }

    // ─────────────────────────────────────────────
    // Material master record (from DB)
    // ─────────────────────────────────────────────
    public class MaterialMasterRow
    {
        public int     ItemID       { get; set; }
        public string  ItemDesc     { get; set; }
        public string  UOM          { get; set; }
        public decimal MaterialCost { get; set; }
        public decimal ServiceCost  { get; set; }
    }

    // ─────────────────────────────────────────────
    // Estimate history (for History tab)
    // ─────────────────────────────────────────────
    public class EstimateHistoryRow
    {
        public int      EstimateID   { get; set; }
        public string   EstimateNo   { get; set; }
        public string   SubmittedBy  { get; set; }
        public System.DateTime SubmittedOn { get; set; }
        public decimal  TotalCost    { get; set; }
        public string   Status       { get; set; }
        public int      ItemCount    { get; set; }
    }

    // ─────────────────────────────────────────────
    // Helper: extension on IEnumerable<EstimateRow>
    // ─────────────────────────────────────────────
    public static class EstimateRowExtensions
    {
        public static decimal Sum(this System.Collections.Generic.IEnumerable<EstimateRow> rows,
                                  System.Func<EstimateRow, decimal> selector)
        {
            decimal sum = 0;
            foreach (var r in rows) sum += selector(r);
            return sum;
        }
        public static int Count(this System.Collections.Generic.IEnumerable<EstimateRow> rows,
                                System.Func<EstimateRow, bool> predicate)
        {
            int count = 0;
            foreach (var r in rows) if (predicate(r)) count++;
            return count;
        }
    }
}
