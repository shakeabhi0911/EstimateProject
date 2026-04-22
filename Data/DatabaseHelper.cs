using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EstimateProject.Models;

namespace EstimateProject.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ─────────────────────────────────────────
        // Get all active material master records
        // ─────────────────────────────────────────
        public List<MaterialMasterRow> GetMaterialMaster()
        {
            var list = new List<MaterialMasterRow>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd  = new SqlCommand("usp_GetMaterialMaster", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MaterialMasterRow
                {
                    ItemID       = reader.GetInt32(0),
                    ItemDesc     = reader.GetString(1),
                    UOM          = reader.GetString(2),
                    MaterialCost = reader.GetDecimal(3),
                    ServiceCost  = reader.GetDecimal(4)
                });
            }
            return list;
        }

        // ─────────────────────────────────────────
        // Submit a full estimate (header + details)
        // Returns the new EstimateID
        // ─────────────────────────────────────────
        public int SubmitEstimate(List<EstimateRow> rows, string estimateNo,
                                  string submittedBy, decimal totalCost, string remarks)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // Insert header
                int estimateId;
                using (var cmd = new SqlCommand("usp_SubmitEstimate", conn, tx))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@EstimateNo",   estimateNo);
                    cmd.Parameters.AddWithValue("@SubmittedBy",  submittedBy);
                    cmd.Parameters.AddWithValue("@TotalCost",    totalCost);
                    cmd.Parameters.AddWithValue("@Remarks",      (object)remarks ?? DBNull.Value);
                    var outParam = new SqlParameter("@EstimateID", SqlDbType.Int)
                                   { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(outParam);
                    cmd.ExecuteNonQuery();
                    estimateId = (int)outParam.Value;
                }

                // Insert details
                foreach (var row in rows)
                {
                    using var cmd2 = new SqlCommand("usp_SubmitEstimateDetail", conn, tx);
                    cmd2.CommandType = CommandType.StoredProcedure;
                    cmd2.Parameters.AddWithValue("@EstimateID",   estimateId);
                    cmd2.Parameters.AddWithValue("@SerialNo",     row.SerialNo);
                    cmd2.Parameters.AddWithValue("@ItemDesc",     row.ItemDesc);
                    cmd2.Parameters.AddWithValue("@UOM",          row.UOM);
                    cmd2.Parameters.AddWithValue("@Quantity",     row.Quantity);
                    cmd2.Parameters.AddWithValue("@MaterialCost", row.MaterialCost);
                    cmd2.Parameters.AddWithValue("@ServiceCost",  row.ServiceCost);
                    cmd2.Parameters.AddWithValue("@TotalCost",    row.TotalCost);
                    cmd2.Parameters.AddWithValue("@IsVerified",   row.IsVerified);
                    cmd2.Parameters.AddWithValue("@MismatchFlag", row.MismatchFlag);
                    cmd2.ExecuteNonQuery();
                }

                tx.Commit();
                return estimateId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ─────────────────────────────────────────
        // Get estimate submission history
        // ─────────────────────────────────────────
        public List<EstimateHistoryRow> GetEstimateHistory()
        {
            var list = new List<EstimateHistoryRow>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd  = new SqlCommand("usp_GetEstimateHistory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new EstimateHistoryRow
                {
                    EstimateID  = reader.GetInt32(0),
                    EstimateNo  = reader.GetString(1),
                    SubmittedBy = reader.GetString(2),
                    SubmittedOn = reader.GetDateTime(3),
                    TotalCost   = reader.GetDecimal(4),
                    Status      = reader.GetString(5),
                    ItemCount   = reader.GetInt32(6)
                });
            }
            return list;
        }

        // ─────────────────────────────────────────
        // Test connection
        // ─────────────────────────────────────────
        public bool TestConnection()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                return true;
            }
            catch { return false; }
        }
    }
}
