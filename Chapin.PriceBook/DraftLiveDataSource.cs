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
                twelveK = reader.GetString(1);  // 'PP2' | 'BM2'
                includeFob = reader.GetBoolean(2);
            }

            // If the generator passes excludeFuturePrices=true, let that override the draft flag:
            var effectiveUseLatest = useLatestInclFuture && !excludeFuturePrices;

            // 2) Main query: section mapping + draft lines + base prices (current/latest)
            var rows = new List<PriceBookRow>();
            List<(string Item, string Desc)> itemsForEnrichment = new();

            await using (var conn = new SqlConnection(_conn))
            {
                await conn.OpenAsync(ct);

                await using var cmd = new SqlCommand("dbo.Chap_PriceBook_Preview", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@DraftId", _draftId);
                cmd.Parameters.AddWithValue("@UseLatest", effectiveUseLatest ? 1 : 0);
                cmd.Parameters.AddWithValue("@FourK", fourK);       // 'PP1' or 'BM1'
                cmd.Parameters.AddWithValue("@TwelveK", twelveK);   // 'PP2' or 'BM2'
                cmd.Parameters.AddWithValue("@IncludeFOB", includeFob ? 1 : 0);

                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                {
                    // Output column order from the proc:
                    // 0: combo_id, 1: display_label, 2: Item, 3: Description,
                    // 4..9: unit_price1..unit_price6
                    string combo = r.IsDBNull(0) ? "WS1-SEC1-SS1-ACC0" : r.GetString(0);
                    var (ws, sec, ss, acc) = ParseCombo(combo);
                    string display = r.IsDBNull(1) ? "" : r.GetString(1);
                    string item = r.IsDBNull(2) ? "" : r.GetString(2);
                    string desc = r.IsDBNull(3) ? "" : r.GetString(3);

                    decimal? up1 = r.IsDBNull(4) ? null : r.GetDecimal(4); // List
                    decimal? up2 = r.IsDBNull(5) ? null : r.GetDecimal(5); // 4k (PP1/BM1 mapped in proc)
                    decimal? up3 = r.IsDBNull(6) ? null : r.GetDecimal(6); // 12.5k (PP2/BM2 mapped in proc)
                    decimal? up4 = r.IsDBNull(7) ? null : r.GetDecimal(7);
                    decimal? up5 = r.IsDBNull(8) ? null : r.GetDecimal(8);
                    decimal? up6 = r.IsDBNull(9) ? null : r.GetDecimal(9); // FOB (optional)

                    // Temporarily set the new fields to defaults; we’ll enrich after we batch-fetch specs.
                    rows.Add(new PriceBookRow(
                        combo, ws, sec, ss, acc, display, item, desc,
                        up1, up2, up3, up4, up5, up6,
                        UPC: "", QTY: 1, masterQty: null, palletQty: null, MSRP: null));

                    // Keep track of items to enrich (use description we already have as a fallback).
                    itemsForEnrichment.Add((item, desc));
                }
            }

            // 3) Enrichment: Pull Chap_ItemSpecs and item_mst descriptions in batch, then compute QTY/UPC/master/pallet.
            if (rows.Count > 0)
            {
                var distinctItems = itemsForEnrichment
                    .Select(x => x.Item)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Build a TVP for items
                var tvp = new DataTable();
                tvp.Columns.Add("Value", typeof(string));
                foreach (var it in distinctItems)
                    tvp.Rows.Add(it);

                var specMap = new Dictionary<string, ItemSpecRow>(StringComparer.OrdinalIgnoreCase);

                await using (var conn = new SqlConnection(_conn))
                {
                    await conn.OpenAsync(ct);

                    const string specSql = @"
-- Items to enrich are passed in @Items (dbo.StringList)
SELECT
    s.Item,
    im.charfld1          AS DefaultUPC,
    s.MasterPackQty     AS MasterPackQty,
    s.PalletQty         AS PalletQty,
    s.GTIN0, s.GTIN1, s.GTIN2, s.GTIN3, s.GTIN4, s.GTIN5, s.GTIN6, s.GTIN7, s.GTIN8,
    s.GTIN0Desc, s.GTIN1Desc, s.GTIN2Desc, s.GTIN3Desc, s.GTIN4Desc, s.GTIN5Desc, s.GTIN6Desc, s.GTIN7Desc, s.GTIN8Desc,
    im.Description      AS ItemDesc
FROM dbo.Chap_ItemSpecs s
LEFT JOIN dbo.item_mst im ON im.item = s.Item
INNER JOIN @Items i ON i.Value = s.Item;";

                    await using var specCmd = new SqlCommand(specSql, conn);
                    var p = specCmd.Parameters.AddWithValue("@Items", tvp);
                    p.SqlDbType = SqlDbType.Structured;
                    p.TypeName = "dbo.StringList";

                    await using var rr = await specCmd.ExecuteReaderAsync(ct);
                    while (await rr.ReadAsync(ct))
                    {
                        var spec = new ItemSpecRow
                        {
                            Item = rr["Item"] as string ?? "",
                            DefaultUPC = rr["DefaultUPC"] as string ?? "",
                            MasterPackQty = SafeGetInt(rr, "MasterPackQty"),
                            PalletQty = SafeGetInt(rr, "PalletQty"),
                            ItemDesc = rr["ItemDesc"] as string ?? "",
                            GTIN = new string?[9],
                            GTINDesc = new string?[9]
                        };

                        // Read GTIN arrays (0..8)
                        for (int i = 0; i <= 8; i++)
                        {
                            spec.GTIN[i] = rr[$"GTIN{i}"] as string;
                            spec.GTINDesc[i] = rr[$"GTIN{i}Desc"] as string;
                        }

                        specMap[spec.Item] = spec;
                    }
                }

                // Helper: extract token between parentheses that starts with a number, e.g., "(6PK)" -> "6PK"
                static string? ExtractParenToken(string? s)
                {
                    if (string.IsNullOrEmpty(s))
                        return null;
                    var m = Regex.Match(s, @"\((?<tok>\d[^)]*)\)");
                    return m.Success ? m.Groups["tok"].Value.Trim() : null;
                }

                static string Normalize(string? s) =>
                    string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

                // Enrich rows
                foreach (var row in rows.ToList())
                {
                    if (!specMap.TryGetValue(row.Item, out var spec))
                        continue;

                    // Prefer item_mst.Description (joined) for token; fall back to the row’s original Description.
                    var sourceDesc = !string.IsNullOrWhiteSpace(spec.ItemDesc) ? spec.ItemDesc : row.Description;
                    var token = ExtractParenToken(sourceDesc);
                    int qty = 1;
                    string upc = spec.DefaultUPC ?? "";

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        // Quantity = leading number of token, e.g., "6PK" or "12 CT" -> 6 / 12
                        var m = Regex.Match(token, @"^(?<num>\d+)");
                        if (m.Success && int.TryParse(m.Groups["num"].Value, out var n))
                            qty = n;

                        // UPC: match token (spaces removed, case-insensitive) against GTINxDesc (also normalized)
                        var normTok = Normalize(token);
                        for (int i = 0; i <= 8; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(spec.GTINDesc[i]) && Normalize(spec.GTINDesc[i]) == normTok)
                            {
                                if (!string.IsNullOrWhiteSpace(spec.GTIN[i]))
                                {
                                    upc = spec.GTIN[i]!;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // No parentheses token → qty=1 already; UPC remains at DefaultUPC (charfld1)
                        qty = 1;
                    }

                    // Rebuild the record with enriched fields (records are immutable)
                    var idx = rows.IndexOf(row);
                    rows[idx] = row with
                    {
                        UPC = upc ?? "",
                        QTY = qty,
                        masterQty = spec.MasterPackQty,
                        palletQty = spec.PalletQty,
                        // MSRP left as null unless you decide its source
                    };
                }
            }

            return rows;
        }
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

    // Local DTO for specs
    sealed class ItemSpecRow
        {
            public string Item { get; set; } = "";
            public string DefaultUPC { get; set; } = "";
            public int? MasterPackQty { get; set; }
            public int? PalletQty { get; set; }
            public string ItemDesc { get; set; } = "";
            public string?[] GTIN { get; set; } = default!;
            public string?[] GTINDesc { get; set; } = default!;
        }



        private int? SafeGetInt(IDataRecord rr, string columnName)
        {
            int ordinal = rr.GetOrdinal(columnName);

            if (rr.IsDBNull(ordinal))
                return null;

            var value = rr[ordinal];
            if (value is int i)
                return i;

            if (int.TryParse(value.ToString(), out int parsed))
                return parsed;

            return null;
        }






        public async Task<IReadOnlyList<PriceBookRow>> GetRowsAsyncOLD(bool excludeFuturePrices, CancellationToken ct)
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

                // Call the stored procedure instead of inlining the CTE SQL
                await using var cmd = new SqlCommand("dbo.Chap_PriceBook_Preview", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@DraftId", _draftId);
                cmd.Parameters.AddWithValue("@UseLatest", effectiveUseLatest ? 1 : 0);
                cmd.Parameters.AddWithValue("@FourK", fourK);       // 'PP1' or 'BM1'
                cmd.Parameters.AddWithValue("@TwelveK", twelveK);   // 'PP2' or 'BM2'
                cmd.Parameters.AddWithValue("@IncludeFOB", includeFob ? 1 : 0);

                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                {
                    // Output column order from the proc:
                    // 0: combo_id, 1: display_label, 2: Item, 3: Description,
                    // 4..9: unit_price1..unit_price6
                    string combo = r.IsDBNull(0) ? "WS1-SEC1-SS1-ACC0" : r.GetString(0);
                    var (ws, sec, ss, acc) = ParseCombo(combo);
                    string display = r.IsDBNull(1) ? "" : r.GetString(1);
                    string item = r.IsDBNull(2) ? "" : r.GetString(2);
                    string desc = r.IsDBNull(3) ? "" : r.GetString(3);

                    decimal? up1 = r.IsDBNull(4) ? null : r.GetDecimal(4); // List
                    decimal? up2 = r.IsDBNull(5) ? null : r.GetDecimal(5); // 4k (PP1/BM1 mapped in proc)
                    decimal? up3 = r.IsDBNull(6) ? null : r.GetDecimal(6); // 12.5k (PP2/BM2 mapped in proc)
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
