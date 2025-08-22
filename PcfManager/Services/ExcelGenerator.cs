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

    public static async Task<byte[]> ExportPcfToExcel(PCFHeaderDTO header)
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
        var itemHeaders = new[] { "ItemNum", "ItemDesc", "Price", "Margin", "Family_Code", "Family_Code_Description", "FY2025_Sales", "FY2025_Units", "FY2024_Sales","FY2024_Units", "FY2023_Sales","FY2023_Units" };

        for (int i = 0; i < itemHeaders.Length; i++)
        {
            worksheet.Range[18, i + 1].Text = itemHeaders[i];
            worksheet.Range[18, i + 1].CellStyle.Font.Bold = true;
        }

        // Populate PCFItemDTO Data
        int rowIndex = 19; // Start writing item data at row 20
        foreach (var item in header.PCFLines.OrderBy(line => line.ItemNum))
        {
            worksheet.Range[rowIndex, 1].Text = item.ItemNum;
            worksheet.Range[rowIndex, 2].Text = item.ItemDesc;
            worksheet.Range[rowIndex, 3].Number = item.ProposedPrice;
            worksheet.Range[rowIndex, 4].Number = (double)item.Margin;
            worksheet.Range[rowIndex, 4].NumberFormat = "0.00%";
            worksheet.Range[rowIndex, 5].Text = item.Family_Code;
            worksheet.Range[rowIndex, 6].Text = item.Family_Code_Description;
            worksheet.Range[rowIndex, 7].Number = (double)item.CurrentFYSales;
            worksheet.Range[rowIndex, 8].Number = (double)item.CurrentFYUnits;
            worksheet.Range[rowIndex, 9].Number = (double)item.Prior1FYSales;
            worksheet.Range[rowIndex, 10].Number = (double)item.Prior1FYUnits;
            worksheet.Range[rowIndex, 11].Number = (double)item.Prior2FYSales;
            worksheet.Range[rowIndex, 12].Number = (double)item.Prior2FYUnits;
         


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



}

