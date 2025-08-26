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
