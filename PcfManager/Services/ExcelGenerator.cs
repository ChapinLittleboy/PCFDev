using PcfManager.Data;
using PcfManager.Models;
using Syncfusion.XlsIO;

namespace PcfManager.Services;
public class ExcelGenerator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DataService _dataService;
    private readonly IUserService _userService;
    private readonly RepRepository _repRepository;
    private readonly CustomerService _customerService;

    public List<PaymentTerm> terms;




    public ExcelGenerator(DataService dataService, IUserService userService, RepRepository repRepository, CustomerService customerService)
    {
        _dataService = dataService;
        _userService = userService;
        _repRepository = repRepository;
        _customerService = customerService;
        terms = _dataService.GetPaymentTerms();


    }

    public static async Task<byte[]> ExportPcfToExcel(PCFHeaderDTO header, IEnumerable<string> itemColumns)
    {
        if (header == null)
            throw new ArgumentNullException(nameof(header));
        if (itemColumns == null)
            throw new ArgumentNullException(nameof(itemColumns));

        using ExcelEngine excelEngine = new ExcelEngine();
        IApplication application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Xlsx;

        IWorkbook workbook = application.Workbooks.Create(1);
        IWorksheet worksheet = workbook.Worksheets[0];

        // ===== Header (unchanged) =====
        worksheet.Range["A1:D1"].Merge();
        worksheet.Range["A1"].Text = $@"PCF {header.PcfNumber} Export";
        worksheet.Range["A1"].CellStyle.Font.Size = 16;
        worksheet.Range["A1"].CellStyle.Font.Bold = true;
        worksheet.Range["A1"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

        worksheet.Range["A2"].Text = "PCF Number:";
        worksheet.Range["B2"].Text = header.PcfNum.ToString();
        worksheet.Range["A3"].Text = "Customer Name (Number):";
        worksheet.Range["B3"].Text = header.CustomerInfo.CustNameWithNum;
        worksheet.Range["A4"].Text = "Effective Dates:";
        worksheet.Range["B4"].Text = $"{header.StartDate:MM/dd/yyyy} to {header.EndDate:MM/dd/yyyy}";
        worksheet.Range["A5"].Text = "Rep ID:";
        worksheet.Range["B5"].Text = header.RepID;
        worksheet.Range["A6"].Text = "Buying Group:";
        worksheet.Range["B6"].Text = header.BuyingGroup;
        worksheet.Range["A7"].Text = "Bill To Info:";
        worksheet.Range["B7"].Text = $"{header.BillToAddress}, {header.BillToCity}, {header.BTState}, {header.BTZip}";
        worksheet.Range["A8"].Text = "Buyer:";
        worksheet.Range["B8"].Text = header.Buyer;
        worksheet.Range["A9"].Text = "PCF Type:";
        worksheet.Range["B9"].Text = header.PcfType;
        worksheet.Range["A10"].Text = "Promo Payment Terms:";
        worksheet.Range["B10"].Text = header.PromoPaymentTerms;
        worksheet.Range["A11"].Text = "Promo Payment Terms Text:";
        worksheet.Range["B11"].Text = header.PromoPaymentTermsText;
        worksheet.Range["A12"].Text = "Freight Terms:";
        worksheet.Range["B12"].Text = header.FreightTerms;
        worksheet.Range["A13"].Text = "Freight Minimums:";
        worksheet.Range["B13"].Text = header.FreightMinimums;
        worksheet.Range["A14"].Text = "General Notes:";
        worksheet.Range["B14"].Text = header.GeneralNotes;
        worksheet.Range["A15"].Text = "Market Type:";
        worksheet.Range["B15"].Text = header.MarketType;
        worksheet.Range["A2:A15"].CellStyle.Font.Bold = true;

        worksheet.Range["A17:B17"].Merge();
        worksheet.Range["A17"].Text = "PCF Item Details:";
        worksheet.Range["A17"].CellStyle.Font.Bold = true;

        // ===== Item section with dynamic columns =====
        int fy = FiscalYearEndYear(DateTime.Now);

        // Build column specs based on requested tokens
        var specs = BuildItemColumnSpecs(itemColumns, fy).ToList();
        if (specs.Count == 0)
            throw new ArgumentException("No valid item columns were provided.", nameof(itemColumns));

        // Write column headers at row 18
        for (int i = 0; i < specs.Count; i++)
        {
            worksheet.Range[18, i + 1].Text = specs[i].Header;
            worksheet.Range[18, i + 1].CellStyle.Font.Bold = true;
        }

        // Write item rows starting row 19
        int rowIndex = 19;
        foreach (var item in header.PCFLines.OrderBy(l => l.ItemNum))
        {
            for (int ci = 0; ci < specs.Count; ci++)
            {
                var cell = worksheet.Range[rowIndex, ci + 1];
                specs[ci].Write(cell, item);
            }
            rowIndex++;
        }

        worksheet.UsedRange.AutofitColumns();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Convenience overload that reproduces today's “all columns” behavior.
    /// </summary>
    public static async Task<byte[]> ExportPcfToExcel(PCFHeaderDTO header)
    {
        var fy = FiscalYearEndYear(DateTime.Now);
        var defaultCols = new[]
        {
        "ItemNum","ItemDesc","ItemStatus","Price","Margin","Family_Code","Family_Code_Description",
        $"FY{fy}_Sales",$"FY{fy}_Units",$"FY{fy-1}_Sales",$"FY{fy-1}_Units",$"FY{fy-2}_Sales",$"FY{fy-2}_Units"
    };
        return await ExportPcfToExcel(header, defaultCols);
    }

    #region Helpers

    private sealed class ColSpec
    {
        public string Header { get; init; } = "";
        public Action<IRange, PCFItemDTO> Write { get; init; } = (_, __) => { };
    }

    /// <summary>
    /// Maps the caller’s requested tokens to column headers and cell writers.
    /// Supports both friendly tokens (e.g., “Price”, “Margin”) and explicit FY headers (e.g., “FY2026_Sales”).
    /// </summary>
    private static IEnumerable<ColSpec> BuildItemColumnSpecsOLD(IEnumerable<string> requested, int fy)
    {
        // Normalize for easy matching, but keep original to render headers when needed.
        var req = requested.Select(s => s?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        foreach (var token in req)
        {
            var key = token.Replace("_", "").ToUpperInvariant();

            // Core fields
            if (key == "ITEMNUM")
                yield return new ColSpec { Header = "ItemNum", Write = (c, x) => c.Text = x.ItemNum };
            else if (key == "ITEMDESC")
                yield return new ColSpec { Header = "ItemDesc", Write = (c, x) => c.Text = x.ItemDesc };
            else if (key == "ITEMSTATUS")
            {
                yield return new ColSpec
                {
                    Header = "ItemStatus",
                    Write = (c, x) =>
                    {
                        var status = (x?.ItemStatus ?? string.Empty).ToUpperInvariant() switch
                        {
                            "A" => "Active",
                            "O" => "Obsolete",
                            "S" => "Slow Moving",
                            _ => ""
                        };
                        c.Text = status;
                    }
                };
            }
            else if (key is "PRICE" or "PROPOSEDPRICE")
                yield return new ColSpec { Header = "Price", Write = (c, x) => c.Number = x.ProposedPrice };
            else if (key == "MARGIN")
            {
                yield return new ColSpec
                {
                    Header = "Margin",
                    Write = (c, x) =>
                    {
                        c.Number = (double)x.Margin;
                        c.NumberFormat = "0.00%";
                    }
                };
            }
            else if (key == "FAMILYCODE")
                yield return new ColSpec { Header = "Family_Code", Write = (c, x) => c.Text = x.Family_Code };
            else if (key == "FAMILYCODEDESCRIPTION")
                yield return new ColSpec { Header = "Family_Code_Description", Write = (c, x) => c.Text = x.Family_Code_Description };

            // Friendly FY aliases (current / prior1 / prior2)
            else if (key is "FYCURRENTSALES" or "CURRENTFYSALES")
                yield return new ColSpec { Header = $"FY{fy}_Sales", Write = (c, x) => c.Number = (double)x.CurrentFYSales };
            else if (key is "FYCURRENTUNITS" or "CURRENTFYUNITS")
                yield return new ColSpec { Header = $"FY{fy}_Units", Write = (c, x) => c.Number = (double)x.CurrentFYUnits };
            else if (key is "FYPRIOR1SALES" or "PRIOR1FYSALES")
                yield return new ColSpec { Header = $"FY{fy - 1}_Sales", Write = (c, x) => c.Number = (double)x.Prior1FYSales };
            else if (key is "FYPRIOR1UNITS" or "PRIOR1FYUNITS")
                yield return new ColSpec { Header = $"FY{fy - 1}_Units", Write = (c, x) => c.Number = (double)x.Prior1FYUnits };
            else if (key is "FYPRIOR2SALES" or "PRIOR2FYSALES")
                yield return new ColSpec { Header = $"FY{fy - 2}_Sales", Write = (c, x) => c.Number = (double)x.Prior2FYSales };
            else if (key is "FYPRIOR2UNITS" or "PRIOR2FYUNITS")
                yield return new ColSpec { Header = $"FY{fy - 2}_Units", Write = (c, x) => c.Number = (double)x.Prior2FYUnits };

            // Explicit FY headers (e.g., "FY2026_Sales" / "FY2026_Units")
            else if (TryParseExplicitFyHeader(token, out var fyYear, out var kind))
            {
                if (kind == "SALES")
                {
                    // Map the requested year to the proper property by offset from current FY
                    yield return new ColSpec
                    {
                        Header = $"FY{fyYear}_Sales",
                        Write = (c, x) =>
                        {
                            if (fyYear == fy)
                                c.Number = (double)x.CurrentFYSales;
                            else if (fyYear == fy - 1)
                                c.Number = (double)x.Prior1FYSales;
                            else if (fyYear == fy - 2)
                                c.Number = (double)x.Prior2FYSales;
                            else
                                c.Text = ""; // outside available range
                        }
                    };
                }
                else // UNITS
                {
                    yield return new ColSpec
                    {
                        Header = $"FY{fyYear}_Units",
                        Write = (c, x) =>
                        {
                            if (fyYear == fy)
                                c.Number = (double)x.CurrentFYUnits;
                            else if (fyYear == fy - 1)
                                c.Number = (double)x.Prior1FYUnits;
                            else if (fyYear == fy - 2)
                                c.Number = (double)x.Prior2FYUnits;
                            else
                                c.Text = ""; // outside available range
                        }
                    };
                }
            }
            // Unknown token: skip silently so the caller can pass a superset if they want
        }
    }

    private static IEnumerable<ColSpec> BuildItemColumnSpecs(IEnumerable<string> requested, int fy)
    {
        var req = requested.Select(s => s?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        foreach (var token in req)
        {
            var key = token.Replace("_", "").ToUpperInvariant();

            // Core fields
            if (key == "ITEMNUM")
                yield return new ColSpec { Header = "ItemNum", Write = (c, x) => c.Text = x.ItemNum };
            else if (key == "ITEMDESC")
                yield return new ColSpec { Header = "ItemDesc", Write = (c, x) => c.Text = x.ItemDesc };
            else if (key == "ITEMSTATUS")
            {
                yield return new ColSpec
                {
                    Header = "ItemStatus",
                    Write = (c, x) =>
                    {
                        var status = (x?.ItemStatus ?? string.Empty).ToUpperInvariant() switch
                        {
                            "A" => "Active",
                            "O" => "Obsolete",
                            "S" => "Slow Moving",
                            _ => ""
                        };
                        c.Text = status;
                    }
                };
            }
            else if (key is "PRICE" or "PROPOSEDPRICE")
            {
                yield return new ColSpec
                {
                    Header = "Price",
                    Write = (c, x) => WriteCurrency(c, (double)x.ProposedPrice)
                };
            }
            else if (key == "MARGIN")
            {
                yield return new ColSpec
                {
                    Header = "Margin",
                    Write = (c, x) =>
                    {
                        c.Number = (double)x.Margin;
                        c.NumberFormat = "0.00%";
                    }
                };
            }
            else if (key == "FAMILYCODE")
                yield return new ColSpec { Header = "Family_Code", Write = (c, x) => c.Text = x.Family_Code };
            else if (key == "FAMILYCODEDESCRIPTION")
                yield return new ColSpec { Header = "Family_Code_Description", Write = (c, x) => c.Text = x.Family_Code_Description };

            // Friendly FY aliases (current / prior1 / prior2)
            else if (key is "FYCURRENTSALES" or "CURRENTFYSALES")
                yield return new ColSpec { Header = $"FY{fy}_Sales", Write = (c, x) => WriteCurrency(c, (double)x.CurrentFYSales) };
            else if (key is "FYCURRENTUNITS" or "CURRENTFYUNITS")
                yield return new ColSpec { Header = $"FY{fy}_Units", Write = (c, x) => c.Number = (double)x.CurrentFYUnits };
            else if (key is "FYPRIOR1SALES" or "PRIOR1FYSALES")
                yield return new ColSpec { Header = $"FY{fy - 1}_Sales", Write = (c, x) => WriteCurrency(c, (double)x.Prior1FYSales) };
            else if (key is "FYPRIOR1UNITS" or "PRIOR1FYUNITS")
                yield return new ColSpec { Header = $"FY{fy - 1}_Units", Write = (c, x) => c.Number = (double)x.Prior1FYUnits };
            else if (key is "FYPRIOR2SALES" or "PRIOR2FYSALES")
                yield return new ColSpec { Header = $"FY{fy - 2}_Sales", Write = (c, x) => WriteCurrency(c, (double)x.Prior2FYSales) };
            else if (key is "FYPRIOR2UNITS" or "PRIOR2FYUNITS")
                yield return new ColSpec { Header = $"FY{fy - 2}_Units", Write = (c, x) => c.Number = (double)x.Prior2FYUnits };

            // Explicit FY headers (e.g., "FY2026_Sales" / "FY2026_Units")
            else if (TryParseExplicitFyHeader(token, out var fyYear, out var kind))
            {
                if (kind == "SALES")
                {
                    yield return new ColSpec
                    {
                        Header = $"FY{fyYear}_Sales",
                        Write = (c, x) =>
                        {
                            if (fyYear == fy)
                                WriteCurrency(c, (double)x.CurrentFYSales);
                            else if (fyYear == fy - 1)
                                WriteCurrency(c, (double)x.Prior1FYSales);
                            else if (fyYear == fy - 2)
                                WriteCurrency(c, (double)x.Prior2FYSales);
                            else
                                c.Text = ""; // outside available range
                        }
                    };
                }
                else // UNITS
                {
                    yield return new ColSpec
                    {
                        Header = $"FY{fyYear}_Units",
                        Write = (c, x) =>
                        {
                            if (fyYear == fy)
                                c.Number = (double)x.CurrentFYUnits;
                            else if (fyYear == fy - 1)
                                c.Number = (double)x.Prior1FYUnits;
                            else if (fyYear == fy - 2)
                                c.Number = (double)x.Prior2FYUnits;
                            else
                                c.Text = ""; // outside available range
                        }
                    };
                }
            }
            // Unknown token: skip
        }
    }

    private static void WriteCurrency(IRange cell, double value)
    {
        cell.Number = value;
        // Currency format with thousands separator and two decimals.
        // (If you prefer accounting format, use: "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)" )
        cell.NumberFormat = "$#,##0.00";
    }
    private static bool TryParseExplicitFyHeader(string token, out int fyYear, out string kind /* SALES or UNITS */)
    {
        fyYear = 0;
        kind = "";
        // Accept forms like "FY2026_Sales" or "FY2026_Units" (case-insensitive)
        // Also tolerate spaces instead of underscore.
        var t = token?.Trim() ?? "";
        var parts = t.Replace(' ', '_').Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;
        if (!parts[0].StartsWith("FY", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!int.TryParse(parts[0].Substring(2), out fyYear))
            return false;

        var k = parts[1].ToUpperInvariant();
        if (k is not ("SALES" or "UNITS"))
            return false;

        kind = k;
        return true;
    }

    #endregion



    public static async Task<byte[]> ExportPcfToExcelOld(PCFHeaderDTO header)
    {
        

        using ExcelEngine excelEngine = new ExcelEngine();
        IApplication application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Xlsx;

        // Create workbook and worksheet
        IWorkbook workbook = application.Workbooks.Create(1);
        IWorksheet worksheet = workbook.Worksheets[0];

        // Write Header Details in the First Section
        worksheet.Range["A1:D1"].Merge();
        worksheet.Range["A1"].Text = $@"PCF {header.PcfNumber} Export";

        worksheet.Range["A1"].CellStyle.Font.Size = 16;
        worksheet.Range["A1"].CellStyle.Font.Bold = true;
        worksheet.Range["A1"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

        worksheet.Range["A2"].Text = "PCF Number:";
        worksheet.Range["B2"].Text = header.PcfNum.ToString();

        worksheet.Range["A3"].Text = "Customer Name (Number):";
        worksheet.Range["B3"].Text = header.CustomerInfo.CustNameWithNum;

        worksheet.Range["A4"].Text = "Effective Dates:";
        string dateRange = $"{header.StartDate:MM/dd/yyyy} to {header.EndDate:MM/dd/yyyy}";
        worksheet.Range["B4"].Text = dateRange;


        worksheet.Range["A5"].Text = "Rep ID:";
        worksheet.Range["B5"].Text = header.RepID;

        worksheet.Range["A6"].Text = "Buying Group:";
        worksheet.Range["B6"].Text = header.BuyingGroup;


        worksheet.Range["A7"].Text = "Bill To Info:";
        worksheet.Range["B7"].Text = $"{header.BillToAddress}, {header.BillToCity}, {header.BTState}, {header.BTZip}";

        worksheet.Range["A8"].Text = "Buyer:";
        worksheet.Range["B8"].Text = header.Buyer;
        worksheet.Range["A9"].Text = "PCF Type:";
        worksheet.Range["B9"].Text = header.PcfType;

        worksheet.Range["A10"].Text = "Promo Payment Terms:";
        worksheet.Range["B10"].Text = header.PromoPaymentTerms;

        worksheet.Range["A11"].Text = "Promo Payment Terms Text:";
        worksheet.Range["B11"].Text = header.PromoPaymentTermsText;

        worksheet.Range["A12"].Text = "Freight Terms:";
        worksheet.Range["B12"].Text = header.FreightTerms;

        worksheet.Range["A13"].Text = "Freight Minimums:";
        worksheet.Range["B13"].Text = header.FreightMinimums;

        worksheet.Range["A14"].Text = "General Notes:";
        worksheet.Range["B14"].Text = header.GeneralNotes;

        worksheet.Range["A15"].Text = "Market Type:";
        worksheet.Range["B15"].Text = header.MarketType;
        worksheet.Range["A2:A15"].CellStyle.Font.Bold = true;


        // Add a separator row
        worksheet.Range["A17:B17"].Merge();
        worksheet.Range["A17"].Text = "PCF Item Details:";
        worksheet.Range["A17"].CellStyle.Font.Bold = true;


        // Write Item Details Starting from Row 19
        var fy = FiscalYearEndYear(DateTime.Now);
        var itemHeaders = new[] { "ItemNum", "ItemDesc", "ItemStatus", "Price", "Margin", "Family_Code", "Family_Code_Description", $"FY{fy}_Sales", $"FY{fy}_Units", $"FY{fy-1}_Sales", $"FY{fy-1}_Units", $"FY{fy-2}_Sales", $"FY{fy-2}_Units" };

        for (int i = 0; i < itemHeaders.Length; i++)
        {
            worksheet.Range[18, i + 1].Text = itemHeaders[i];
            worksheet.Range[18, i + 1].CellStyle.Font.Bold = true;
        }

        // Populate PCFItemDTO Data
        int rowIndex = 19; // Start writing item data at row 20
        foreach (var item in header.PCFLines.OrderBy(line => line.ItemNum))
        {
            var statusText = (item?.ItemStatus ?? string.Empty).ToUpperInvariant() switch
            {
                "A" => "Active",
                "O" => "Obsolete",
                "S" => "Slow Moving",
                _ => ""
            };

            worksheet.Range[rowIndex, 1].Text = item.ItemNum;
            worksheet.Range[rowIndex, 2].Text = item.ItemDesc;
            worksheet.Range[rowIndex, 3].Text = statusText;
            worksheet.Range[rowIndex, 4].Number = item.ProposedPrice;
            worksheet.Range[rowIndex, 5].Number = (double)item.Margin;
            worksheet.Range[rowIndex, 5].NumberFormat = "0.00%";
            worksheet.Range[rowIndex, 6].Text = item.Family_Code;
            worksheet.Range[rowIndex, 7].Text = item.Family_Code_Description;
            worksheet.Range[rowIndex, 8].Number = (double)item.CurrentFYSales;
            worksheet.Range[rowIndex, 9].Number = (double)item.CurrentFYUnits;
            worksheet.Range[rowIndex, 10].Number = (double)item.Prior1FYSales;
            worksheet.Range[rowIndex, 11].Number = (double)item.Prior1FYUnits;
            worksheet.Range[rowIndex, 12].Number = (double)item.Prior2FYSales;
            worksheet.Range[rowIndex, 13].Number = (double)item.Prior2FYUnits;




            //worksheet.Range[rowIndex, 6].Number = item.PP1Price;
            // worksheet.Range[rowIndex, 7].Number = item.PP2Price;

            rowIndex++;
        }

        worksheet.UsedRange.AutofitColumns();

        // Save workbook to memory stream
        using MemoryStream stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();

    }




    private void FormatCell(IWorksheet sheet, string cellAddress, int fontSize, bool bold = false, ExcelHAlign hAlign = ExcelHAlign.HAlignLeft)
    {
        var cell = sheet[cellAddress];
        cell.CellStyle.Font.Size = fontSize;
        cell.CellStyle.Font.Bold = bold;
        cell.CellStyle.HorizontalAlignment = hAlign;
    }

    private void FormatCellText(IWorksheet sheet, string cellAddress, string cellText, int fontSize = 12, bool bold = false, ExcelHAlign hAlign = ExcelHAlign.HAlignRight)
    {
        var cell = sheet[cellAddress];
        cell.Text = cellText;
        cell.CellStyle.Font.Size = fontSize;
        cell.CellStyle.Font.Bold = bold;
        cell.CellStyle.HorizontalAlignment = hAlign;
    }

    private static int FiscalYearEndYear(DateTime asOf)
        => asOf.Month >= 9 ? asOf.Year + 1 : asOf.Year;

}

