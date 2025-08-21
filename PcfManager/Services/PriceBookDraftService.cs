// Services/PriceBookDraftService.cs
using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;

namespace PcfManager.Services
{
    // Match this to your grid row model
    public sealed class ItemPricePbDto
    {
        public string ItemNum { get; set; } = "";
        public string? ItemDesc { get; set; }
        public string? Family_Code { get; set; }

        public decimal? ListPrice { get; set; }
        public decimal? PP1Price { get; set; }
        public decimal? PP2Price { get; set; }
        public decimal? BM1Price { get; set; }
        public decimal? BM2Price { get; set; }
        public decimal? FOBPrice { get; set; }

        public decimal? NewListPrice { get; set; }
        public decimal? NewPP1Price { get; set; }
        public decimal? NewPP2Price { get; set; }
        public decimal? NewBM1Price { get; set; }
        public decimal? NewBM2Price { get; set; }
        public decimal? NewFOBPrice { get; set; }
    }

    public interface IPriceBookDraftService
    {
        Task<long> SaveDraftAsync(
            string mode,
            bool useLatestInclFuture,
            string createdBy,
            IEnumerable<ItemPricePbDto> rows);
    }

    public sealed class PriceBookDraftService : IPriceBookDraftService
    {
        private readonly string _connectionString;

        public PriceBookDraftService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<long> SaveDraftAsync(
            string mode,
            bool useLatestInclFuture,
            string createdBy,
            IEnumerable<ItemPricePbDto> rows)
        {
            if (string.IsNullOrWhiteSpace(mode))
                throw new ArgumentException("mode is required", nameof(mode));
            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("createdBy is required", nameof(createdBy));
            if (rows is null)
                throw new ArgumentNullException(nameof(rows));

            var tvp = BuildDraftDataTable(rows);

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("Chap_PriceBook_SaveDraft", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@Mode", SqlDbType.VarChar, 20) { Value = mode });
            cmd.Parameters.Add(new SqlParameter("@UseLatestInclFuture", SqlDbType.Bit) { Value = useLatestInclFuture });
            cmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.VarChar, 200) { Value = createdBy });

            // TVP parameter
            var linesParam = cmd.Parameters.AddWithValue("@Lines", tvp);
            linesParam.SqlDbType = SqlDbType.Structured;
            linesParam.TypeName = "Chap_ItemPriceDraftType"; // must match the type name in SQL

            // OUTPUT parameter
            var outParam = new SqlParameter("@DraftId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outParam);

            await cmd.ExecuteNonQueryAsync();

            return (long)(outParam.Value ?? 0L);
        }

        private static DataTable BuildDraftDataTable(IEnumerable<ItemPricePbDto> rows)
        {
            var table = new DataTable();
            // Columns must match the TVP definition exactly (name + order + type)
            table.Columns.Add("ItemNum", typeof(string));
            table.Columns.Add("ItemDesc", typeof(string));
            table.Columns.Add("Family_Code", typeof(string));

            table.Columns.Add("ListPrice", typeof(decimal));
            table.Columns.Add("PP1Price", typeof(decimal));
            table.Columns.Add("PP2Price", typeof(decimal));
            table.Columns.Add("BM1Price", typeof(decimal));
            table.Columns.Add("BM2Price", typeof(decimal));
            table.Columns.Add("FOBPrice", typeof(decimal));

            table.Columns.Add("NewListPrice", typeof(decimal));
            table.Columns.Add("NewPP1Price", typeof(decimal));
            table.Columns.Add("NewPP2Price", typeof(decimal));
            table.Columns.Add("NewBM1Price", typeof(decimal));
            table.Columns.Add("NewBM2Price", typeof(decimal));
            table.Columns.Add("NewFOBPrice", typeof(decimal));

            foreach (var r in rows)
            {
                table.Rows.Add(
                    r.ItemNum,
                    (object?)r.ItemDesc ?? DBNull.Value,
                    (object?)r.Family_Code ?? DBNull.Value,

                    (object?)r.ListPrice ?? DBNull.Value,
                    (object?)r.PP1Price ?? DBNull.Value,
                    (object?)r.PP2Price ?? DBNull.Value,
                    (object?)r.BM1Price ?? DBNull.Value,
                    (object?)r.BM2Price ?? DBNull.Value,
                    (object?)r.FOBPrice ?? DBNull.Value,

                    (object?)r.NewListPrice ?? DBNull.Value,
                    (object?)r.NewPP1Price ?? DBNull.Value,
                    (object?)r.NewPP2Price ?? DBNull.Value,
                    (object?)r.NewBM1Price ?? DBNull.Value,
                    (object?)r.NewBM2Price ?? DBNull.Value,
                    (object?)r.NewFOBPrice ?? DBNull.Value
                );
            }

            return table;
        }
    }
}
