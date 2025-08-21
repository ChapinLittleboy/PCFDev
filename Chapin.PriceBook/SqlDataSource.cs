using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Chapin.PriceBook;

public sealed class SqlDataSource : IDataSource
{
    private readonly string _conn;
    public string Key => "sql";
    public SqlDataSource(string connectionString) => _conn = connectionString;

    public async Task<IReadOnlyList<PriceBookRow>> GetRowsAsync(bool excludeFuturePrices, CancellationToken ct)
    {
        var list = new List<PriceBookRow>();
        await using var conn = new SqlConnection(_conn);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
WITH LatestPrice AS (
    SELECT
        p.Item,
        p.unit_price1, --List
        p.unit_price2, -- PP1
        p.unit_price3, -- PP2
        p.unit_price4,  --BM1
        p.unit_price5,   --BM2
        p.unit_price6,   --FOB
        p.effect_date,
        ROW_NUMBER() OVER (
            PARTITION BY p.Item
            ORDER BY p.effect_date DESC
        ) AS rn
    FROM dbo.ItemPrice_mst AS p
    -- If you only want prices that are already effective, uncomment:
    -- WHERE p.effect_date <= GETDATE()
),
S AS (
    SELECT
        pbs.combo_id,
        pbs.display_label,
        im.Item,
        im.Uf_CustomerFriendlyDescription as [Description]
    FROM dbo.Chap_PriceBookSections AS pbs
    INNER JOIN dbo.item_mst AS im
        ON im.Uf_PriceBookSection = pbs.combo_id
)
SELECT
    s.combo_id,
    s.display_label,
    s.Item,
    s.[Description],
    lp.unit_price1, --List Price
    lp.unit_price2,  --PP1
       lp.unit_price3,  --PP2
       lp.unit_price4,  --BM1
        lp.unit_price5,   --BM2
       lp.unit_price6,   --FOB
FROM S AS s
LEFT JOIN LatestPrice AS lp
    ON lp.Item = s.Item
   AND lp.rn = 1
ORDER BY
    s.combo_id,
    s.Item;";

        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            string combo = r.GetString(0);
            var (ws, sec, ss, acc) = ParseCombo(combo);
            string display = r.IsDBNull(1) ? "" : r.GetString(1);
            string item = r.IsDBNull(2) ? "" : r.GetString(2);
            string desc = r.IsDBNull(3) ? "" : r.GetString(3);
            decimal? up1 = r.IsDBNull(4) ? null : r.GetDecimal(4);
            decimal? up2 = r.IsDBNull(5) ? null : r.GetDecimal(5);
            decimal? up3 = r.IsDBNull(6) ? null : r.GetDecimal(6);
            decimal? up4 = r.IsDBNull(7) ? null : r.GetDecimal(7);  // currently unused
            decimal? up5 = r.IsDBNull(8) ? null : r.GetDecimal(8);  // currently unused
            decimal? up6 = r.IsDBNull(9) ? null : r.GetDecimal(9);  // currently unused

            list.Add(new PriceBookRow(combo, ws, sec, ss, acc, display, item, desc, up1, up2, up3, up4, up5, up6));
        }

        return list;
    }

    private static (int WS, int Sec, int SS, int Acc) ParseCombo(string combo)
    {
        var m = Regex.Match(combo ?? "", @"^WS(?<ws>\d+)-SEC(?<sec>\d+)-SS(?<ss>\d+)-ACC(?<acc>\d+)$",
            RegexOptions.IgnoreCase);
        if (!m.Success)
            return (1, 1, 1, 0);
        return (int.Parse(m.Groups["ws"].Value),
                int.Parse(m.Groups["sec"].Value),
                int.Parse(m.Groups["ss"].Value),
                int.Parse(m.Groups["acc"].Value));
    }
}
