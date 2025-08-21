// Chapin.PriceBook/DraftLiveDataSource.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Chapin.PriceBook
{
    /// <summary>
    /// IDataSource that reads directly from the draft tables for a "live" preview.
    /// - Pulls New* values from Chap_PriceBookDraftLine (grid snapshot).
    /// - Falls back to base prices (Current or Latest incl. future) depending on the draft header flag.
    /// - Applies the template mapping: FourKSource (PP1/BM1), TwelveKSource (PP2/BM2), IncludeFOB.
    /// - Maps into PriceBookRow: List -> unit_price1, 4k -> unit_price2 (PP1 slot), 12.5k -> unit_price3 (PP2 slot).
    /// </summary>
    public sealed class DraftLiveDataSource : IDataSource
    {
        private readonly string _conn;
        private readonly long _draftId;
        private readonly int _templateId;

        /// <param name="key">Optional key to expose to PriceBookRequest.SourceKey (defaults to $"draft-live-{draftId}-{templateId}")</param>
        public DraftLiveDataSource(string connectionString, long draftId, int templateId, string? key = null)
        {
            _conn = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _draftId = draftId;
            _templateId = templateId;
            Key = key ?? $"draft-live-{draftId}-{templateId}";
        }

        public string Key { get; }

        public async Task<IReadOnlyList<PriceBookRow>> GetRowsAsync(bool excludeFuturePrices, CancellationToken ct)
        {
            // 1) Load header flag and template mapping
            bool useLatestInclFuture;
            string fourK;      // 'PP1' or 'BM1'
            string twelveK;    // 'PP2' or 'BM2'
            bool includeFob;

            await using (var conn = new SqlConnection(_conn))
            {
                await conn.OpenAsync(ct);

                const string metaSql = @"
SELECT h.UseLatestInclFuture
FROM dbo.Chap_PriceBookDraftHeader h
WHERE h.DraftId = @DraftId;

SELECT t.FourKSource, t.TwelveKSource, t.IncludeFOB
FROM dbo.Chap_PriceBookTemplate t
WHERE t.TemplateId = @TemplateId;";

                await using var metaCmd = new SqlCommand(metaSql, conn);
                metaCmd.Parameters.AddWithValue("@DraftId", _draftId);
                metaCmd.Parameters.AddWithValue("@TemplateId", _templateId);

                using var reader = await metaCmd.ExecuteReaderAsync(ct);

                if (!await reader.ReadAsync(ct))
                    throw new InvalidOperationException($"DraftId {_draftId} not found.");
                useLatestInclFuture = reader.GetBoolean(0);

                if (!await reader.NextResultAsync(ct) || !await reader.ReadAsync(ct))
                    throw new InvalidOperationException($"TemplateId {_templateId} not found.");
                fourK = reader.GetString(0);    // 'PP1' | 'BM1'
                twelveK = reader.GetString(1);    // 'PP2' | 'BM2'
                includeFob = reader.GetBoolean(2);
            }

            // If the generator passes excludeFuturePrices=true, let that override the draft flag:
            var effectiveUseLatest = useLatestInclFuture && !excludeFuturePrices;

            // 2) Main query: section mapping + draft lines + base prices (current/latest)
            var rows = new List<PriceBookRow>();
            await using (var conn = new SqlConnection(_conn))
            {
                await conn.OpenAsync(ct);

                // We LEFT JOIN both base views and choose one with CASE to keep the plan stable.
                var sql = @"
WITH S AS (
    SELECT
        pbs.combo_id,
        pbs.display_label,
        im.item,
        im.Uf_CustomerFriendlyDescription AS [Description]
    FROM dbo.item_mst AS im
    INNER JOIN dbo.Chap_PriceBookSections AS pbs
        ON im.Uf_PriceBookSection = pbs.combo_id
    WHERE im.active_for_customer_portal = 1
),
L AS (
    SELECT
        l.ItemNum,
        l.ItemDesc,
        l.Family_Code,
        l.NewListPrice, l.NewPP1Price, l.NewPP2Price, l.NewBM1Price, l.NewBM2Price, l.NewFOBPrice
    FROM dbo.Chap_PriceBookDraftLine AS l
    WHERE l.DraftId = @DraftId
),
BC AS (   -- Base Current (exclude future)
    SELECT c.item,
           c.ListPrice, c.PP1Price, c.PP2Price, c.BM1Price, c.BM2Price, c.FOBPrice
    FROM dbo.v_ItemPrice_Current c
),
BL AS (   -- Base Latest (may include future)
    SELECT b.item,
           b.ListPrice, b.PP1Price, b.PP2Price, b.BM1Price, b.BM2Price, b.FOBPrice
    FROM dbo.v_ItemPrice_Latest b
),
E AS (
    SELECT
        l.ItemNum,
        -- Effective values: grid New* wins; otherwise choose Current vs Latest based on effectiveUseLatest
        COALESCE(l.NewListPrice, CASE WHEN @UseLatest = 1 THEN BL.ListPrice ELSE BC.ListPrice END) AS ListPrice,
        COALESCE(l.NewPP1Price,  CASE WHEN @UseLatest = 1 THEN BL.PP1Price  ELSE BC.PP1Price  END) AS PP1,
        COALESCE(l.NewPP2Price,  CASE WHEN @UseLatest = 1 THEN BL.PP2Price  ELSE BC.PP2Price  END) AS PP2,
        COALESCE(l.NewBM1Price,  CASE WHEN @UseLatest = 1 THEN BL.BM1Price  ELSE BC.BM1Price  END) AS BM1,
        COALESCE(l.NewBM2Price,  CASE WHEN @UseLatest = 1 THEN BL.BM2Price  ELSE BC.BM2Price  END) AS BM2,
        COALESCE(l.NewFOBPrice,  CASE WHEN @UseLatest = 1 THEN BL.FOBPrice  ELSE BC.FOBPrice  END) AS FOB
    FROM L
    LEFT JOIN BL ON BL.item = l.ItemNum
    LEFT JOIN BC ON BC.item = l.ItemNum
)
SELECT
    s.combo_id,
    s.display_label,
    s.item                             AS Item,
    COALESCE(s.[Description], l.ItemDesc) AS [Description],
    e.ListPrice                        AS unit_price1,
    CASE WHEN @FourK = 'PP1' THEN e.PP1 ELSE e.BM1 END AS unit_price2,   -- 4k -> PP1 slot
    CASE WHEN @TwelveK = 'PP2' THEN e.PP2 ELSE e.BM2 END AS unit_price3, -- 12.5k -> PP2 slot
    CAST(NULL AS decimal(18,4))        AS unit_price4,
    CAST(NULL AS decimal(18,4))        AS unit_price5,
    CASE WHEN @IncludeFOB = 1 THEN e.FOB ELSE NULL END AS unit_price6
FROM E e
LEFT JOIN S ON S.item = e.ItemNum
LEFT JOIN L l ON l.ItemNum = e.ItemNum
ORDER BY s.combo_id, s.item;";

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@DraftId", _draftId);
                cmd.Parameters.AddWithValue("@UseLatest", effectiveUseLatest ? 1 : 0);
                cmd.Parameters.AddWithValue("@FourK", fourK);       // 'PP1' or 'BM1'
                cmd.Parameters.AddWithValue("@TwelveK", twelveK);   // 'PP2' or 'BM2'
                cmd.Parameters.AddWithValue("@IncludeFOB", includeFob ? 1 : 0);

                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                {
                    string combo = r.IsDBNull(0) ? "WS1-SEC1-SS1-ACC0" : r.GetString(0);
                    var (ws, sec, ss, acc) = ParseCombo(combo);
                    string display = r.IsDBNull(1) ? "" : r.GetString(1);
                    string item = r.IsDBNull(2) ? "" : r.GetString(2);
                    string desc = r.IsDBNull(3) ? "" : r.GetString(3);

                    decimal? up1 = r.IsDBNull(4) ? null : r.GetDecimal(4); // List
                    decimal? up2 = r.IsDBNull(5) ? null : r.GetDecimal(5); // 4k -> PP1 slot
                    decimal? up3 = r.IsDBNull(6) ? null : r.GetDecimal(6); // 12.5k -> PP2 slot
                    decimal? up4 = r.IsDBNull(7) ? null : r.GetDecimal(7);
                    decimal? up5 = r.IsDBNull(8) ? null : r.GetDecimal(8);
                    decimal? up6 = r.IsDBNull(9) ? null : r.GetDecimal(9); // FOB (optional)

                    rows.Add(new PriceBookRow(combo, ws, sec, ss, acc, display, item, desc, up1, up2, up3, up4, up5, up6));
                }
            }

            return rows;

            static (int WS, int Sec, int SS, int Acc) ParseCombo(string? combo)
            {
                var m = Regex.Match(combo ?? "", @"^WS(?<ws>\d+)-SEC(?<sec>\d+)-SS(?<ss>\d+)-ACC(?<acc>\d+)$", RegexOptions.IgnoreCase);
                if (!m.Success)
                    return (1, 1, 1, 0);
                return (int.Parse(m.Groups["ws"].Value),
                        int.Parse(m.Groups["sec"].Value),
                        int.Parse(m.Groups["ss"].Value),
                        int.Parse(m.Groups["acc"].Value));
            }
        }
    }
}
