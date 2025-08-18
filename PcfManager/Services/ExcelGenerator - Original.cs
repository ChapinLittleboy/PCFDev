using BlazorServerDatagridApp2.Data;
using BlazorServerDatagridApp2.Models;
using Syncfusion.Office;
using Syncfusion.XlsIO;

namespace BlazorServerDatagridApp2.Services;
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
        var itemHeaders = new[] { "ItemNum", "ItemDesc", "Price", "Family_Code", "Family_Code_Description" };
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
            worksheet.Range[rowIndex, 4].Text = item.Family_Code;
            worksheet.Range[rowIndex, 5].Text = item.Family_Code_Description;

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



    // public async Task<FileStreamResult> CreatePricingWorkbookForDownload(string selectedCustomer, string selectedPCID)
    public async Task<byte[]> CreatePricingWorkbookForDownload(string selectedCustomer, string selectedPCID)
    {

        using (ExcelEngine excelEngine = new ExcelEngine())
        {
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;

            // Workbook setup
            IWorkbook workbook = application.Workbooks.Create(5);
            IWorksheet headerSheet = workbook.Worksheets[0];
            headerSheet.Name = "SPR Header";
            IWorksheet detailsSheet = workbook.Worksheets[1];
            detailsSheet.Name = "SPR Details";
            IWorksheet termsListSheet = workbook.Worksheets[2];
            termsListSheet.Name = "TermsList";
            IWorksheet itemListSheet = workbook.Worksheets[3];
            itemListSheet.Name = "ItemList";
            IWorksheet lySalesListSheet = workbook.Worksheets[4];
            lySalesListSheet.Name = "LYSalesList";

            // Populate sheets



            PopulateSPRHeader(headerSheet, selectedCustomer);
            PopulateSPRDetails(detailsSheet, selectedPCID);
            PopulateTermsList(termsListSheet);
            PopulateItemListAsync(itemListSheet);
            PopulateLYSalesList(lySalesListSheet, selectedCustomer);


            termsListSheet.Visibility = WorksheetVisibility.Hidden;
            itemListSheet.Visibility = WorksheetVisibility.Hidden;
            lySalesListSheet.Visibility = WorksheetVisibility.Hidden;


            //  Add a macro
            IVbaModule worksheetModule = workbook.VbaProject.Modules[detailsSheet.CodeName];

            string macroCode = @"
        Private Sub Worksheet_Change(ByVal Target As Range)
            If Not Intersect(Target, Me.Columns(""A"")) Is Nothing Then
                If Target.Value <> """" Then
                   Application.EnableEvents = False
                    Target.NumberFormat = ""@""
                    Target.Offset(1, 0).NumberFormat = ""@""
                    Target.Offset(0, 2).Formula = ""=IFERROR(VLOOKUP(A"" & Target.Row & "", ItemList!A:D, 4, FALSE), """""""")""
                    Target.Offset(0, 4).Formula = ""=IFERROR(VLOOKUP(A"" & Target.Row & "", ItemList!A:D, 2, FALSE), """""""")""
                    Target.Offset(0, 6).Formula = ""=IFERROR(VLOOKUP(A"" & Target.Row & "", LYSalesList!B:D, 2, FALSE), """""""")""
                    Target.Offset(0, 7).Formula = ""=IFERROR(VLOOKUP(A"" & Target.Row & "", LYSalesList!B:D, 3, FALSE), """""""")""
                    Target.Offset(0, 8).Formula = ""=IFERROR(VLOOKUP(A"" & Target.Row & "", ItemList!A:J, 8, FALSE), """""""")""
                    Target.Offset(0, 9).Formula = ""=IFERROR(VLOOKUP(A"" & Target.Row & "", ItemList!A:J, 9, FALSE), """""""")""

                    Target.Offset(0,1).NumberFormat = ""#,##0.00""
                    Target.Offset(0,2).NumberFormat = ""#,##0.00""
                    Target.Offset(0,5).NumberFormat = ""#,##0.00""
                    Target.Offset(0,6).NumberFormat = ""#,##0""
                    Target.Offset(0,7).NumberFormat = ""$#,##0""
                    Application.EnableEvents = True
                End If
            End If
        End Sub";
            worksheetModule.Code = macroCode;

            var x = workbook.Names["TermsDescriptions"].RefersToRange;
            IDataValidation termDataValidation = headerSheet.Range["D10"].DataValidation;
            termDataValidation.DataRange = x;
            termDataValidation.ShowErrorBox = true;
            termDataValidation.PromptBoxText = "Choose Terms from the dropdown.";
            termDataValidation.ErrorBoxText = "Invalid selection. Please choose a valid option.";

            headerSheet.Activate();
            headerSheet.Range["D7"].Activate();

            // Set sheet visibility
            // termsListSheet.Visibility = WorksheetVisibility.StrongHidden;
            // itemListSheet.Visibility = WorksheetVisibility.StrongHidden;
            // lySalesListSheet.Visibility = WorksheetVisibility.StrongHidden;

            // Save to memory stream
            using (MemoryStream stream = new MemoryStream())
            {
                workbook.SaveAs(stream, ExcelSaveType.SaveAsMacro);
                //stream.Position = 0;

                // Return as FileStreamResult for download
                //               return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                //               {
                //                   FileDownloadName = "PCF_Template.xlsx"
                //               };
                return stream.ToArray();
            }
        }
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



    private void PopulateSPRHeader(IWorksheet sheet, string selectedCustomer)
    {
        // Get customer information asynchronously
        var customer = _customerService.GetCustomerInformationForRepCustomer(selectedCustomer);

        // Add the static text to the sheet
        sheet["A1"].Text = "";  // Fake data so I can set column widths
        sheet["B1"].Text = "";
        sheet["C1"].Text = "";
        sheet["D1"].Text = "";
        sheet["E1"].Text = "";

        sheet.Range["A:A"].ColumnWidth = 25;
        sheet.Range["B:B"].ColumnWidth = 25;
        sheet.Range["C:C"].ColumnWidth = 25;
        sheet.Range["D:D"].ColumnWidth = 30;
        sheet.Range["E:E"].ColumnWidth = 25;


        //sheet["c1"].Text = "PCF Request Template";

        FormatCellText(sheet, "C1", "PCF Request Template", 20, true, ExcelHAlign.HAlignCenter);
        FormatCellText(sheet, "B3", "PCF Number", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C3", "not yet assigned", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "D3", "Status", 12, true, ExcelHAlign.HAlignRight);
        sheet[ExcelCellReferences.PCFNumberCell].CellStyle.Font.Color = ExcelKnownColors.Red;
        sheet[ExcelCellReferences.PCFStatusCell].CellStyle.Font.Color = ExcelKnownColors.Red;
        FormatCellText(sheet, "E3", "New request", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "A7", "Customer Number", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "A8", "Customer Name", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "A9", "Buying Group", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "A10", "Customer Terms", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "A11", "Customer Status", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "A12", "Pricing Method", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "B14", "Bill To Information", 14, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "D14", "Ship To Information (if different)", 14, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "A15", "Name", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "A16", "Address 1", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "a17", "Address 2", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "a18", "City", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "A19", "State", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "A20", "Zip", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C15", "Name", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C16", "Address 1", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C17", "Address 2", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C18", "City", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C19", "State", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C20", "Zip", 12, true, ExcelHAlign.HAlignRight);

        FormatCellText(sheet, "C7", "Effective Date", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C8", "Expiration Date", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C9", "PCF Type", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C10", "Promo PCF Terms", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C24", "Rep Code", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C25", "Rep Name", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C26", "Rep Agency", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C27", "Rep Email", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C28", "Rep Phone", 12, true, ExcelHAlign.HAlignRight);
        sheet.Range["B20:D20"].NumberFormat = "@";  // set to text format for zipcode field

        // Get the description for the Payment Terms
        var paymentTermsService = new PaymentTermsService(terms);


        // Fill in the Data!

        sheet["B7"].Text = customer.CustNum;
        sheet["B8"].Text = customer.CustName;
        sheet["B9"].Text = customer.BuyingGroup;
        sheet["B10"].Text = paymentTermsService.GetDescriptionByCode(customer.PaymentTerms);
        sheet["B12"].Text = customer.PricingMethod;

        sheet["B15"].Text = customer.BillToName;  // For some reason this is empty
        sheet["B15"].Text = customer.CustName;
        sheet["B16"].Text = customer.BillToAddress1;
        sheet["B17"].Text = customer.BillToAddress2;
        sheet["B18"].Text = customer.BillToCity;
        sheet["B19"].Text = customer.BillToState;
        sheet["B20"].Text = customer.BillToZip;


        sheet["B11"].Text = string.IsNullOrEmpty(customer.Status)
            ? "Unknown Status"
            : Enum.TryParse(customer.Status, out Customer.CustomerStatus statusEnum)
                ? statusEnum.GetDescription()
                : "Unknown Status";
        //sheet["B11"].Text = customer.Status;

        // Get sales rep information
        var repID = _userService.CurrentRep.RepId;
        var rep = _repRepository.GetRepById(repID);

        sheet["D24"].Text = rep.RepCode;
        sheet["D25"].Text = rep.Name;
        sheet["D26"].Text = rep.Agency;
        sheet["D27"].Text = rep.Email;


        string[] dropdownValues = { "Warehouse", "Dropship", "Other Warehouse", "Other Dropship", "Other" };
        IDataValidation dataValidation = sheet.Range["D9"].DataValidation;
        dataValidation.ListOfValues = dropdownValues; // Option 1: Inline values
        dataValidation.ShowErrorBox = true;
        dataValidation.PromptBoxText = "Choose an option from the dropdown.";
        dataValidation.ErrorBoxText = "Invalid selection. Please choose a valid option.";

        ;



    }

    private void PopulateSPRDetails(IWorksheet sheet, string selectedPCID)
    {
        // Get item and price details

        sheet.Range["A:A"].ColumnWidth = 15;
        sheet.Range["B:B"].ColumnWidth = 15;
        sheet.Range["C:C"].ColumnWidth = 15;
        sheet.Range["D:D"].ColumnWidth = 5;
        sheet.Range["E:E"].ColumnWidth = 40;
        sheet.Range["F:F"].ColumnWidth = 20;
        sheet.Range["G:G"].ColumnWidth = 15;
        sheet.Range["H:H"].ColumnWidth = 15;
        sheet.Range["I:I"].ColumnWidth = 20;
        sheet.Range["J:J"].ColumnWidth = 30;
        sheet.Range["K:K"].ColumnWidth = 40;


        sheet.Range["B3:H3"].Merge();
        FormatCellText(sheet, "B3", "When you enter the item in column A, the Description, Book Price and Prior 12 month sales will be automatically filled in if known.", 12, true, ExcelHAlign.HAlignLeft);
        sheet.Range["B4:H4"].Merge();
        FormatCellText(sheet, "B4", "Enter your Proposed Price in column B and any notes in column I.", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "A7", "Item", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "B7", "Proposed Price", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "E7", "Description", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "F7", "Previous PCF Price", 12, true, ExcelHAlign.HAlignRight);
        //       FormatCellText(sheet, "F7", "CMA Price", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "C7", "BOOK Price", 12, true, ExcelHAlign.HAlignRight);
        sheet.Range["C8:C257"].CellStyle.Font.Color = ExcelKnownColors.Blue;
        FormatCellText(sheet, "G7", "Units", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "H7", "Dollars", 12, true, ExcelHAlign.HAlignRight);
        FormatCellText(sheet, "I7", "Family Code", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "J7", "Family Description", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "K7", "Notes", 12, true, ExcelHAlign.HAlignLeft);
        sheet.Range["G6:H6"].Merge();
        FormatCellText(sheet, "G6", "Last 12 mo Sales", 12, true, ExcelHAlign.HAlignCenter);
        sheet.Range["A1:A11"].NumberFormat = "@";   // Set to TEXT
        //sheet.Range["B8:F100"].NumberFormat = "#,##0.00";  //Set to Numeric
        //sheet.Range["G8:G100"].NumberFormat = "#,##0";  //Set to Numeric no decimal
        //sheet.Range["H8:H100"].NumberFormat = "$#,##0";  //Set to Numeric Dollar no decimal

        var items = _dataService.GetPcfItemsAndPrices(selectedPCID);
        int startRow = 8;
        foreach (var item in items)
        {
            sheet[$"A{startRow}"].Text = item.ItemNum;
            sheet[$"B{startRow}"].Number = item.ProposedPrice;
            sheet[$"F{startRow}"].Number = item.ProposedPrice;  // PCF ref price
            sheet[$"C{startRow}"].Formula = $"=IFERROR(VLOOKUP(A{startRow}, ItemList!A:D, 4, FALSE), \"\")"; //Book Price
            sheet[$"E{startRow}"].Formula = $"=IFERROR(VLOOKUP(A{startRow}, ItemList!A:D, 2, FALSE), \"\")"; //Description
            sheet[$"B{startRow}"].NumberFormat = "#,##0.00";
            sheet[$"C{startRow}"].NumberFormat = "#,##0.00";
            sheet[$"F{startRow}"].NumberFormat = "#,##0.00";
            sheet[$"G{startRow}"].NumberFormat = "#,##0";
            sheet[$"H{startRow}"].NumberFormat = "$#,##0";
            sheet[$"G{startRow}"].Formula = $"=IFERROR(VLOOKUP(A{startRow}, LYSalesList!B:D, 2, FALSE), \"\")"; //LY Units
            sheet[$"H{startRow}"].Formula = $"=IFERROR(VLOOKUP(A{startRow}, LYSalesList!B:D, 3, FALSE), \"\")";  //LY Dollars
            sheet[$"I{startRow}"].Formula = $"=IFERROR(VLOOKUP(A{startRow}, ItemList!A:J, 8, FALSE), \"\")";  //Famcode
            sheet[$"J{startRow}"].Formula = $"=IFERROR(VLOOKUP(A{startRow}, ItemList!A:J, 9, FALSE), \"\")";  //Famcode description
            sheet[$"A{startRow + 1}"].NumberFormat = "@";


            startRow++;
        }
        /*
        for (int i = 8; i <= 100; i++)  // lookup for Description
        {
            // This formula only applies VLOOKUP if column A is not empty, else it leaves the cell blank
            sheet.Range["E" + i].Formula = $"=IF(A{i}<>\"\", VLOOKUP(A{i}, ItemList!A:D, 2, FALSE), \"\")"; //Description
            sheet.Range["C" + i].Formula = $"=IF(A{i}<>\"\", VLOOKUP(A{i}, ItemList!A:D, 4, FALSE), \"\")"; //Book Price
           //           sheet.Range["G" + i].Formula = $"=IF(A{i}<>\"\", VLOOKUP(A{i}, LYSalesList!B:D, 2, FALSE), \"\")"; //LY Units
           //           sheet.Range["H" + i].Formula = $"=IF(A{i}<>\"\", VLOOKUP(A{i}, LYSalesList!B:D, 3, FALSE), \"\")"; //LY Dollars
        }
        */


    }

    private void PopulateTermsList(IWorksheet sheet)
    {
        // Get terms list

        sheet.Range["A:A"].ColumnWidth = 25;
        FormatCellText(sheet, "A1", "Description", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "B1", "Terms Code", 12, true, ExcelHAlign.HAlignLeft);
        int startRow = 2;
        foreach (var term in terms)
        {
            sheet[$"A{startRow}"].Text = term.Description;
            sheet[$"B{startRow}"].Text = term.Terms_Code;
            startRow++;
        }

        // Also add PCF Types to use for DataValidation

        // Set up the dropdown list values
        string[] dropdownValues = { "Warehouse", "Promo" };

        sheet.Range["F1"].Text = "PCF Type Description";
        sheet.Range["F2"].Text = "Warehouse";
        sheet.Range["F3"].Text = "Promo";   // change range below also
        sheet.Names.Add("PCFTypeDescriptions", sheet.Range["F2:F3"]);


        int lastRow = startRow - 1;
        sheet.Workbook.Names.Add("TermsDescriptions", sheet.Range[$"A2:A{lastRow}"]);


    }

    private async Task PopulateItemListAsync(IWorksheet sheet)
    {
        // Get item list from SQL query via DataService
        sheet.Range["B1"].ColumnWidth = 25;  // was B:B
        FormatCellText(sheet, "A1", "Item", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "B1", "Description", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "C1", "EffectiveDate", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "D1", "Book_Price", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "H1", "Family_Code", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "I1", "Family_Code_Description", 12, true, ExcelHAlign.HAlignLeft);

        var items = await _dataService.GetItemBookPricesAsync(); // Await the task to get the result
        int startRow = 2;
        foreach (var item in items)
        {
            sheet[$"A{startRow}"].Text = item.Item;
            sheet[$"B{startRow}"].Text = item.Description;
            sheet[$"C{startRow}"].DateTime = item.EffectiveDate;
            sheet[$"D{startRow}"].Number = item.BookPrice;
            sheet[$"H{startRow}"].Text = item.Family_Code;
            sheet[$"I{startRow}"].Text = item.Family_Code_Description;
            startRow++;
        }

        sheet.Visibility = WorksheetVisibility.Hidden;
    }

    private void PopulateLYSalesList(IWorksheet sheet, string selectedCustomer)
    {
        // Get last year's sales list


        FormatCellText(sheet, "A1", "CustNum", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "B1", "Item", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "C1", "Quantity", 12, true, ExcelHAlign.HAlignLeft);
        FormatCellText(sheet, "D1", "Dollars", 12, true, ExcelHAlign.HAlignLeft);
        var sales = _dataService.GetSalesByItemForCustomer(selectedCustomer);
        int startRow = 2;
        foreach (var sale in sales)
        {
            sheet[$"A{startRow}"].Text = sale.CustNum;
            sheet[$"B{startRow}"].Text = sale.Item;
            sheet[$"C{startRow}"].Number = sale.InvQty;
            sheet[$"D{startRow}"].Number = sale.InvDollars;
            startRow++;
        }
    }
}

