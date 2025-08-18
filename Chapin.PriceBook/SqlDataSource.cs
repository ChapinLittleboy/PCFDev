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
SELECT
    s.combo_id,
    s.display_label,
    s.Item,
    COALESCE(NULLIF(LTRIM(RTRIM(im.Uf_CustomerFriendlyDescription)), ''), s.[Description]) AS [Description],
    lp.unit_price1,
    lp.unit_price2,
    lp.unit_price3
FROM dbo.Chap_PriceBookSections AS s
LEFT JOIN dbo.Item_mst AS im
    ON LTRIM(RTRIM(im.Item)) = LTRIM(RTRIM(s.Item))
OUTER APPLY (
    SELECT TOP (1)
        p.unit_price1, p.unit_price2, p.unit_price3, p.effect_date
    FROM dbo.ItemPrice_mst AS p
    WHERE LTRIM(RTRIM(p.Item)) = LTRIM(RTRIM(s.Item))
      {(excludeFuturePrices ? "AND p.effect_date <= GETDATE()" : "")}
    ORDER BY p.effect_date DESC
) AS lp
ORDER BY s.combo_id, s.Item;
";

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

            list.Add(new PriceBookRow(combo, ws, sec, ss, acc, display, item, desc, up1, up2, up3));
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
