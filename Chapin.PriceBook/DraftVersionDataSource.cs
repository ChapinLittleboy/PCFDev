using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.SqlClient;


// Chapin.PriceBook/DraftVersionDataSource.cs
namespace Chapin.PriceBook
{
    /// <summary>
    /// IDataSource which pulls from the frozen Chap_PriceBookVersion (+ Lines).
    /// Maps VersionLine.ListPrice -> PriceBookRow.ListPrice,
    ///       VersionLine.Price4k   -> PriceBookRow.PP1 (generator prints "PPD $4000"),
    ///       VersionLine.Price12k  -> PriceBookRow.PP2 (generator prints "PPD $12,500").
    /// </summary>
    public sealed class DraftVersionDataSource : IDataSource
    {
        private readonly string _conn;
        private readonly long _versionId;

        /// <param name="key">A unique key you will use in PriceBookRequest.SourceKey, e.g. $"draft-{versionId}"</param>
        public DraftVersionDataSource(string connectionString, long versionId, string? key = null)
        {
            _conn = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _versionId = versionId;
            Key = key ?? $"draft-{versionId}";
        }

        public string Key { get; }

        public async Task<IReadOnlyList<PriceBookRow>> GetRowsAsync(bool excludeFuturePrices, CancellationToken ct)
        {
            // future/current doesn’t matter here: Version is already frozen.
            var list = new List<PriceBookRow>();

            await using var conn = new SqlConnection(_conn);
            await conn.OpenAsync(ct);

            // We join VersionLine -> item_mst -> Chap_PriceBookSections so we get the
            // same "combo_id" and "display_label" shape as the legacy SqlDataSource.
            var sql = @"
WITH S AS (
    SELECT
        pbs.combo_id,
        pbs.display_label,
        im.Item,
        im.Uf_CustomerFriendlyDescription AS [Description]
    FROM dbo.item_mst AS im
    INNER JOIN dbo.Chap_PriceBookSections AS pbs
        ON im.Uf_PriceBookSection = pbs.combo_id
),
L AS (
    SELECT
        l.ItemNum,
        l.ItemDesc,
        l.Family_Code,
        l.ListPrice,
        l.Price4k,
        l.Price12k,
        l.FOBPrice
    FROM dbo.Chap_PriceBookVersionLine AS l
    WHERE l.VersionId = @VersionId
)
SELECT
    s.combo_id,
    s.display_label,
    s.Item,
    COALESCE(s.[Description], l.ItemDesc) AS [Description],
    l.ListPrice        AS unit_price1, 
    l.Price4k          AS unit_price2, 
    l.Price12k AS unit_price3, 
    NULL AS unit_price4,  --not used by current generator
    NULL AS unit_price5,
    l.FOBPrice AS unit_price6

            
FROM L
LEFT JOIN S ON S.Item = L.ItemNum
ORDER BY s.combo_id, s.Item;
            ";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@VersionId", _versionId);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                string combo = r.IsDBNull(0) ? "WS1-SEC1-SS1-ACC0" : r.GetString(0);
                var (ws, sec, ss, acc) = ParseCombo(combo);
                string display = r.IsDBNull(1) ? "" : r.GetString(1);
                string item = r.IsDBNull(2) ? "" : r.GetString(2);
                string desc = r.IsDBNull(3) ? "" : r.GetString(3);
                decimal? up1 = r.IsDBNull(4) ? null : r.GetDecimal(4); // List
                decimal? up2 = r.IsDBNull(5) ? null : r.GetDecimal(5); // 4k (mapped to PP1 slot)
                decimal? up3 = r.IsDBNull(6) ? null : r.GetDecimal(6); // 12.5k (mapped to PP2 slot)
                decimal? up4 = r.IsDBNull(7) ? null : r.GetDecimal(7);
                decimal? up5 = r.IsDBNull(8) ? null : r.GetDecimal(8);
                decimal? up6 = r.IsDBNull(9) ? null : r.GetDecimal(9); // FOB (unused by current generator)

                list.Add(new PriceBookRow(combo, ws, sec, ss, acc, display, item, desc, up1, up2, up3, up4, up5, up6));
            }

            return list;

            static (int WS, int Sec, int SS, int Acc) ParseCombo(string combo)
            {
                var m = System.Text.RegularExpressions.Regex.Match(combo ?? "", @"^WS(?<ws>\d+)-SEC(?<sec>\d+)-SS(?<ss>\d+)-ACC(?<acc>\d+)$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
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
