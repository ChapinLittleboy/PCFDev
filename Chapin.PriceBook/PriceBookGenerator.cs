using System.Text.RegularExpressions;
using Syncfusion.XlsIO;

namespace Chapin.PriceBook;

public sealed class PriceBookGenerator : IPriceBookGenerator
{
    private readonly Dictionary<string, IDataSource> _sources;

    // Template layout:
    private const int HeaderRow = 2;
    private const int TemplateSectionRow = 3;
    private const int TemplateSubsectionRow = 4;
    private const int TemplateItemRow = 5;

    public PriceBookGenerator(IEnumerable<IDataSource> sources)
    {
        _sources = sources.ToDictionary(s => s.Key.ToLowerInvariant());
    }

    public async Task<byte[]> GenerateAsync(PriceBookRequest req, CancellationToken ct = default)
    {
        if (!_sources.TryGetValue(req.SourceKey.ToLowerInvariant(), out var src))
            throw new InvalidOperationException($"Unknown source '{req.SourceKey}'");

        var rows = await src.GetRowsAsync(req.ExcludeFuturePrices, ct);
        var all = rows;

        using var engine = new ExcelEngine();
        var app = engine.Excel;
        app.DefaultVersion = ExcelVersion.Xlsx;

        using var input = File.OpenRead(req.TemplatePath);
        var wb = app.Workbooks.Open(input, ExcelOpenType.Automatic);
        int? lastSectionWritten = null;


        foreach (var wsGroup in all.GroupBy(r => r.WS).OrderBy(g => g.Key))
            {
                // Preferred sheet name from first row's display_label
                var names = SplitDisplay(wsGroup.First().DisplayLabel);
                var preferredSheetName = names.Sheet ?? $"WS{wsGroup.Key:00}";

                // Get or create worksheet for this WS##
                var sheet = GetOrCreateWorksheet(wb, wsGroup.Key, preferredSheetName);

                // Find anchor if present; else default to A3
                var (anchorSheet, anchorRow, anchorCol) = FindDefinedName(wb, $"ANCHOR_WS{wsGroup.Key:00}");
                if (anchorSheet != null && anchorSheet != sheet)
                    Console.WriteLine($"WARN: ANCHOR_WS{wsGroup.Key:00} found on different sheet; using that sheet.");

                sheet = anchorSheet ?? sheet;
                int startRow = anchorRow > 0 ? anchorRow : TemplateSectionRow;
                int startCol = anchorCol > 0 ? anchorCol : 1;
                // Where to write the header text (use your fixed column; if you
                // anchor at C3, set startCol accordingly; leaving as startCol here)
                int sectionCol = 3;
                int subsectionCol = 2;


                // Rename sheet to preferred name if needed
                if (!string.Equals(sheet.Name, preferredSheetName, StringComparison.OrdinalIgnoreCase))
                    sheet.Name = MakeUniqueName(wb, preferredSheetName);

                // Build header map from row 2
                var header = BuildHeaderMap(sheet, HeaderRow);
                //LogResolvedColumns(header);

                // Clear everything BELOW the template rows (row 6..end)
                ClearBelowTemplate(sheet, TemplateItemRow + 1);

                // We'll use the sample styles from the template rows
                int current = startRow;

                foreach (var secGroup in wsGroup.GroupBy(r => r.Sec).OrderBy(g => g.Key))
                {
                    foreach (var ssGroup in secGroup.GroupBy(r => r.SS).OrderBy(g => g.Key))
                    {
                        foreach (var accGroup in ssGroup.GroupBy(r => r.Acc).OrderBy(g => g.Key))
                        {
                            var items = accGroup.ToList();
                            if (items.Count == 0)
                                continue;

                            // Labels
                            var parts = SplitDisplay(items[0].DisplayLabel);
                            var sectionText = parts.Section ?? $"SEC {secGroup.Key:00}";
                            var subsectionText = parts.Subsection ?? $"SS {ssGroup.Key:00}";
                            if (accGroup.Key > 0)
                                subsectionText = parts.Accessories ?? $"{subsectionText} – ACCESSORIES";

                            bool isNewSection = lastSectionWritten != secGroup.Key;

                            // Change this to the correct column index for your template
                            // If section/subsection titles should be in column C, and C is col index 3:


                            int firstDataRow;

                            if (isNewSection)
                            {
                                // --- Write SECTION + SUBSECTION ---
                                if (current == TemplateSectionRow)
                                {
                                    // Reuse template rows 3 & 4
                                    sheet[current, sectionCol].Text = sectionText;            // Section row
                                    sheet[current + 1, subsectionCol].Text = subsectionText;  // Subsection row
                                    firstDataRow = TemplateItemRow;                           // Row 5
                                }
                                else
                                {
                                    // Insert 2 new rows for Section & Subsection
                                    sheet.InsertRow(current, 2);
                                    CopyRowStyle(sheet, TemplateSectionRow, current);         // Section style
                                    CopyRowStyle(sheet, TemplateSubsectionRow, current + 1);  // Subsection style
                                    EnsureSubsectionMerge(sheet, current + 1);

                                    sheet[current, sectionCol].Text = sectionText;
                                    sheet[current + 1, subsectionCol].Text = subsectionText;

                                    firstDataRow = current + 2;
                                }

                                lastSectionWritten = secGroup.Key; // Mark section as written
                            }
                            else
                            {
                                // --- Write ONLY SUBSECTION ---
                                if (current == TemplateSectionRow)
                                {
                                    sheet[TemplateSubsectionRow, subsectionCol].Text = subsectionText;
                                    firstDataRow = TemplateItemRow;
                                }
                                else
                                {
                                    sheet.InsertRow(current, 1);
                                    CopyRowStyle(sheet, TemplateSubsectionRow, current);
                                    EnsureSubsectionMerge(sheet, current);
                                    sheet[current, subsectionCol].Text = subsectionText;
                                    firstDataRow = current + 1;
                                }
                            }

                            // --- Ensure at least one item row ---
                            if (firstDataRow != TemplateItemRow)
                            {
                                sheet.InsertRow(firstDataRow);
                                CopyRowStyle(sheet, TemplateItemRow, firstDataRow);
                            }

                            // Insert additional rows so total = items.Count
                            if (items.Count > 1)
                                sheet.InsertRow(firstDataRow + 1, items.Count - 1);

                            // Copy style to all item rows
                            for (int i = 0; i < items.Count; i++)
                                CopyRowStyle(sheet, TemplateItemRow, firstDataRow + i);

                            // Populate item rows
                            for (int i = 0; i < items.Count; i++)
                            {
                                int r = firstDataRow + i;
                                var row = items[i];

                                Set(sheet, r, header, "ITEM", row.Item);
                                Set(sheet, r, header, "DESCRIPTION", row.Description);
                                Set(sheet, r, header, "LIST PRICE", row.ListPrice);
                                Set(sheet, r, header, "PPD $4000", row.PP1);
                                Set(sheet, r, header, "PPD $12,500", row.PP2);
                            }

                            // Optional: apply currency formats for the three price columns
                            ApplyCurrencyFormat(sheet, header, firstDataRow, firstDataRow + items.Count - 1,
                                new[] { "LIST PRICE", "PPD $4000", "PPD $12,500" });

                            // --- Spacer row between blocks ---
                            int spacerRow = firstDataRow + items.Count; // first row AFTER the items
                            sheet.InsertRow(spacerRow + 1);             // insert exactly one spacer row
                            int sr = spacerRow + 1;

                            // match the item row height so spacing looks consistent
                            sheet.Range[sr, 1].RowHeight = sheet.Range[TemplateItemRow, 1].RowHeight;

                            // wipe borders/fills/fonts/etc. and any accidental values
                            var rng = sheet.Range[sr, 1, sr, sheet.UsedRange.LastColumn];
                            rng.Clear(ExcelClearOptions.ClearFormat | ExcelClearOptions.ClearContent);

                            // advance pointer to the first row for the NEXT header
                            current = sr + 0;
                        }


                    }
                }

            }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        wb.Close();
        return ms.ToArray();
    }

    // ----------------- Helpers (Excel) -----------------
    private static (IWorksheet? Sheet, int Row, int Col) FindDefinedName(IWorkbook wb, string name)
    {
        IName? found = null;
        try
        { found = wb.Names[name]; }
        catch { }
        if (found == null)
        {
            foreach (var obj in wb.Names)
                if (obj is IName nm && string.Equals(nm.Name, name, StringComparison.OrdinalIgnoreCase))
                { found = nm; break; }
        }

        if (found?.RefersToRange is IRange r)
            return (r.Worksheet, r.Row, r.Column);
        return (null, -1, -1);
    }

    private static IWorksheet GetOrCreateWorksheet(IWorkbook wb, int wsNumber, string preferredName)
    {
        var byName = wb.Worksheets.FirstOrDefault(s => string.Equals(s.Name, preferredName, StringComparison.OrdinalIgnoreCase));
        if (byName != null)
            return byName;

        int idx = Math.Max(0, wsNumber - 1);
        if (idx < wb.Worksheets.Count)
            return wb.Worksheets[idx];

        var clone = wb.Worksheets.AddCopy(wb.Worksheets[0]);
        clone.Name = MakeUniqueName(wb, preferredName);
        return clone;
    }

    private static string MakeUniqueName(IWorkbook wb, string desired)
    {
        var baseName = desired;
        int n = 1;
        while (wb.Worksheets.Any(s => string.Equals(s.Name, desired, StringComparison.OrdinalIgnoreCase)))
            desired = $"{baseName} ({++n})";
        return desired;
    }

    private static Dictionary<string, int> BuildHeaderMap(IWorksheet sheet, int headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int lastCol = sheet.UsedRange.LastColumn;
        for (int c = 1; c <= lastCol; c++)
        {
            var raw = sheet[headerRow, c].Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(raw))
                map[NormalizeHeader(raw)] = c;
        }
        return map;
    }

    private static string NormalizeHeader(string s) =>
        new string(s.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static IEnumerable<string> HeaderSynonyms(string logicalName)
    {
        switch (logicalName.ToUpperInvariant())
        {
            case "ITEM":
                return new[] { "ITEM", "ITEM NO.", "ITEMNO", "ITEM#", "ITEMNUMBER", "ITEM\nNO." };
            case "DESCRIPTION":
                return new[] { "DESCRIPTION", "PRODUCT DESCRIPTION", "PRODUCT\nDESCRIPTION" };
            case "LIST PRICE":
                return new[] { "LIST PRICE", "LISTPRICE", "LIST\nPRICE", "PRICE" };
            case "PPD $4000":
                return new[] { "PPD $4000", "PP1", "PPD 4000", "PPD$4000" };
            case "PPD $12,500":
                return new[] { "PPD $12,500", "PP2", "PPD 12500", "PPD$12500", "PPD $12 500" };
            default:
                return new[] { logicalName };
        }
    }

    private static bool TryResolveHeader(Dictionary<string, int> map, string logicalName, out int col)
    {
        foreach (var alias in HeaderSynonyms(logicalName))
            if (map.TryGetValue(NormalizeHeader(alias), out col))
                return true;
        col = -1;
        return false;
    }

    private static void Set(IWorksheet sheet, int row, Dictionary<string, int> header, string logicalHeader, object? value)
    {
        if (!TryResolveHeader(header, logicalHeader, out var c))
            return;
        if (value is null)
        { sheet[row, c].Clear(); return; }

        switch (value)
        {
            case decimal d:
                sheet[row, c].Number = (double)d;
                break;
            case double f:
                sheet[row, c].Number = f;
                break;
            default:
                sheet[row, c].Text = value.ToString();
                break;
        }
    }

    private static void ApplyCurrencyFormat(IWorksheet sheet, Dictionary<string, int> header, int r1, int r2, IEnumerable<string> names)
    {
        foreach (var name in names)
            if (TryResolveHeader(header, name, out var c))
                sheet.Range[r1, c, r2, c].NumberFormat = "$#,##0.00";
    }

    private static void ClearBelowTemplate(IWorksheet sheet, int fromRow)
    {
        int lastRow = sheet.UsedRange.LastRow;
        if (fromRow <= lastRow)
            sheet.DeleteRow(fromRow, lastRow - fromRow + 1);
    }

    private static void CopyRowStyle(IWorksheet sheet, int fromRow, int toRow)
    {
        int lastCol = sheet.UsedRange.LastColumn;
        sheet.Range[toRow, 1].RowHeight = sheet.Range[fromRow, 1].RowHeight;
        IRange src = sheet.Range[fromRow, 1, fromRow, lastCol];
        IRange dst = sheet.Range[toRow, 1, toRow, lastCol];
        src.CopyTo(dst, ExcelCopyRangeOptions.CopyStyles); // styles only (no merges)
    }

    private static void EnsureSubsectionMerge(IWorksheet sheet, int row)
    {
        // Template uses B..C merged for subsection title; adjust here if marketing changes it.
        var rng = sheet.Range[row, 2, row, 3]; // B..C
        if (!rng.IsMerged)
            rng.Merge();
    }

    // ----------------- Helpers (labels) -----------------
    private static (string? Sheet, string? Section, string? Subsection, string? Accessories) SplitDisplay(string display)
    {
        if (string.IsNullOrWhiteSpace(display))
            return (null, null, null, null);
        var parts = display.Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return (parts.ElementAtOrDefault(0), parts.ElementAtOrDefault(1), parts.ElementAtOrDefault(2), parts.ElementAtOrDefault(3));
    }
}
