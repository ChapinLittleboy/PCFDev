using FileProcessingLib.Database;
using Microsoft.Extensions.Configuration;
using Syncfusion.Licensing;
using Syncfusion.XlsIO;

namespace FileProcessingLib;
public class ExcelToPcfProcessor
{

    private string _erpConnectionString;
    private string _pcfConnectionString;
    private string _excelPath;

    public ExcelToPcfProcessor()
    {
        SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXZccHVWQ2NZUkd+XkA=");
    }

    public ProcessResult ProcessExcelFile(string filePath)
    {

        //        _erpConnectionString = AppSettings.ConnectionStrings.CiiSQL10ro;
        //        _pcfConnectionString = AppSettings.ConnectionStrings.BatPCF;
        //        _excelPath = AppSettings.FilePathSettings.SharedUploadsPath;
        _erpConnectionString = "Data Source=ciisql10;Database=BAT_App;User Id=ReportUser1;Password='ReportUser1';TrustServerCertificate=True;";
        _pcfConnectionString = "Data Source=ciisql01;Initial Catalog=custinfo_zbldev;User Id=pcfdev;Password='*id10t*';TrustServerCertificate=True;";
        _excelPath = "c:\\SharedUploads";

        ProcessResult pcfResult;

        if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

        using (var excelEngine = new ExcelEngine())
        {
            var application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;

            // Open the file
            using (var inputStream = new FileStream(filePath, FileMode.Open))
            {
                var workbook = application.Workbooks.Open(inputStream);

                // Access the first worksheet
                var worksheet = workbook.Worksheets[0];

                // Read data
                if (ValidateExcelFile(workbook))
                {
                    Console.WriteLine("Workbook validation passed.");
                }

                pcfResult = CreatePCF(workbook);



                // Close the workbook
                workbook.Close();
            }

            return pcfResult;
        }
    }




    public ProcessResult CreatePCF(IWorkbook workbook)
    {
        string repCode;
        string custNum;
        ProcessResult result;
        //PCFHeaderEntity pcfHeader;
        int pcfNum = -1;
        IWorksheet headerWorksheet = workbook.Worksheets[0];
        IWorksheet detailWorksheet = workbook.Worksheets[1];
        List<PCFItemEntity> pcfItems;
        DateTime startDate;
        DateTime endDate;

        string Standard_Terms;
        string Standard_Terms_Text;
        string Standard_Freight_Terms = "PPD to US Border";
        string Freight_Minimums = "Other";
        string repName;

        string Warehouse = string.Empty;
        string Dropship = string.Empty;
        string OtherDropship = string.Empty;
        string OtherWarehouse = string.Empty;
        string DWOther = string.Empty;
        string DWOtherText = string.Empty;
        string pcfType = string.Empty;

        pcfType = headerWorksheet.Range["D9"].DisplayText;

        //  pcfType = "Dropship";   //for testing
        switch (pcfType)
        {
            case "Warehouse":
                Warehouse = "checked";
                Dropship = OtherDropship = DWOther = DWOtherText = string.Empty;
                break;

            case "Dropship":
                Dropship = "checked";
                Warehouse = OtherDropship = DWOther = DWOtherText = string.Empty;
                break;

            case "Other Warehouse":
                DWOther = "checked";
                Warehouse = Dropship = OtherDropship = DWOtherText = string.Empty;
                break;

            case "Other Dropship":
                OtherDropship = "checked";
                Warehouse = Dropship = DWOther = DWOtherText = string.Empty;
                break;

            case "Other":
                DWOther = "checked";
                DWOtherText = "TBD";
                Warehouse = Dropship = OtherDropship = string.Empty;
                break;

            default:
                // Optionally handle unexpected values
                Warehouse = Dropship = OtherDropship = DWOther = DWOtherText = string.Empty;
                break;
        }



        custNum = headerWorksheet.Range["B7"].Text;



        DatabaseService databaseService = new DatabaseService(_erpConnectionString, _pcfConnectionString);

        Customer customer = databaseService.GetCustomerInformationForCustomer(custNum);

        customer.CustNum = customer.CustNum.Trim();

        PCFHeaderEntity pcfHeader = new PCFHeaderEntity();
        pcfHeader.Warehouse = Warehouse;
        pcfHeader.Dropship = Dropship;
        pcfHeader.OtherDropship = OtherDropship;
        pcfHeader.OtherWarehouse = OtherWarehouse;
        pcfHeader.DWOther = DWOther;
        pcfHeader.DWOtherText = DWOtherText;
        pcfHeader.Promo_Terms_Text = headerWorksheet.Range["D10"].Text;
        pcfHeader.Standard_Terms_Text = headerWorksheet.Range["B10"].Text;
        pcfHeader.SRNum = headerWorksheet.Range["D24"].Text;  //repCode
        repCode = pcfHeader.SRNum;
        repName = headerWorksheet.Range["D25"].Text;

        pcfHeader.STName = headerWorksheet.Range["D15"].Text;
        pcfHeader.STAddr = headerWorksheet.Range["D16"].Text;
        pcfHeader.STCity = headerWorksheet.Range["D18"].Text;
        pcfHeader.STState = headerWorksheet.Range["D19"].Text;
        pcfHeader.STZip = headerWorksheet.Range["D20"].Text;


        object cellValue = headerWorksheet.Range["D7"].Value;


        if (cellValue != null && DateTime.TryParse(cellValue.ToString(), out DateTime dateValue))
        {
            // Successfully parsed the date
            Console.WriteLine($"The start date is: {dateValue.ToShortDateString()}");
            pcfHeader.ProgSDate = dateValue;
        }
        else
        {
            // Handle cases where the cell does not contain a valid date
            Console.WriteLine("The cell does not contain a valid date.");
        }




        cellValue = headerWorksheet.Range["D8"].Value;

        if (cellValue != null && DateTime.TryParse(cellValue.ToString(), out DateTime dateValue2))
        {
            // Successfully parsed the date
            Console.WriteLine($"The end date is: {dateValue2.ToShortDateString()}");
            pcfHeader.ProgEDate = dateValue2;
        }
        else
        {
            // Handle cases where the cell does not contain a valid date
            Console.WriteLine("The cell does not contain a valid date.");
        }

        Rep rep = databaseService.GetRepByRepcode(repCode, repName);





        var CreatedPCFNumber = databaseService.CreatePCFHeader(pcfHeader, customer, rep);

        if (CreatedPCFNumber != null)
        {
            Console.WriteLine(CreatedPCFNumber);
        }



        if (CreatedPCFNumber > 20000)
        {
            int linesInserted = 0;
            pcfItems = GetPCFItems(detailWorksheet, CreatedPCFNumber.ToString(), customer.CustNum);
            if (pcfItems.Count() > 0)
            {
                linesInserted = databaseService.CreatePCFLines(pcfItems);

            }

            if (linesInserted != pcfItems.Count())
            {
                Console.WriteLine("Counts don't match");
            }


            Console.WriteLine($"Lines inserted: {linesInserted}");

        }



        return new ProcessResult
        {
            Status = ProcessStatus.Success,
            PcfNumber = CreatedPCFNumber
        };

    }





    /*   public ProcessResult ProcessExcel(string filepath)
       {
           if (string.IsNullOrWhiteSpace(filepath) || !File.Exists(filepath))
               return new ProcessResult
               {
                   Status = ProcessStatus.Failure,
                   Errors = new List<string> { "File path is invalid or file does not exist." }
               };

           try
           {
               // Process the file
               // Example: Read data, transform, save to DB
               ProcessExcelFile(filepath);

               return new ProcessResult
               {
                   Status = ProcessStatus.Success,
                   PcfNumber = 1
               };
           }
           catch (Exception ex)
           {
               // Handle and log the error
               return new ProcessResult
               {
                   Status = ProcessStatus.Failure,
                   Errors = new List<string> { ex.Message },
                   PcfNumber = -1

               };
           }
       }*/


    public bool ValidateExcelFile(IWorkbook workBook)
    {
        // Access worksheets
        var headerSheet = workBook.Worksheets[0];
        var detailSheet = workBook.Worksheets[1];

        // Initialize validation result


        // Initialize validation result
        var result = true;

        // Retrieve values from the header sheet
        var repCode = headerSheet.Range["D15"].Text;
        var custNum = headerSheet.Range["B7"].Text;

        // Safely parse dates
        DateTime startDateTime;
        DateTime endDateTime;

        if (!DateTime.TryParse(headerSheet.Range["D7"].Value, out startDateTime))
        {
            Console.WriteLine("Invalid start date in cell D7.");
            var x = headerSheet.Range["D7"].Text;
            result = false;
        }


        if (!DateTime.TryParse(headerSheet.Range["D8"].Value, out endDateTime))
        {
            Console.WriteLine("Invalid end date in cell D8.");
            result = false;
        }

        // Perform validations
        if (!ValidateRep(repCode))
        {
            Console.WriteLine("Invalid rep code in cell D15.");
            result = false;
        }

        if (!ValidateCustNum(custNum))
        {
            Console.WriteLine("Invalid customer number in cell B7.");
            result = false;
        }

        if (!ValidateDates(startDateTime, endDateTime))
        {
            Console.WriteLine("Start date and end date validation failed.");
            result = false;
        }

        return result;
    }







    // Helper validation methods
    private bool ValidateRep(string repCode)
    {
        // Add logic to validate the rep code
        return !string.IsNullOrWhiteSpace(repCode);
    }

    private bool ValidateCustNum(string custNum)
    {
        // Add logic to validate the customer number
        return !string.IsNullOrWhiteSpace(custNum);
    }

    private bool ValidateDates(DateTime startDate, DateTime endDate)
    {
        // Add logic to validate date ranges
        return startDate <= endDate;
    }




    public List<PCFItemEntity> GetPCFItems(IWorksheet detailWorksheet, string pcfNumber, string custNum)
    {
        var pcfItems = new List<PCFItemEntity>();

        int startRow = 8; // Data starts at row 8
        int currentRow = startRow;

        while (true)
        {
            // Read values from the worksheet
            var itemNum = detailWorksheet[$"A{currentRow}"].Text?.Trim();
            // Stop if ItemNum (Column A) is empty
            if (string.IsNullOrEmpty(itemNum))
                break;
            var proposedPriceValue = detailWorksheet[$"B{currentRow}"].Value;
            //var itemDesc = detailWorksheet[$"E{currentRow}"].Text.Trim();
            string itemDesc = detailWorksheet.Range[$"E{currentRow}"].DisplayText;
            //var itemDesc = detailWorksheet[$"E{currentRow}"].CalculatedValue?.Trim() ?? string.Empty;



            // Validate and parse ProposedPrice
            double proposedPrice = 0;
            if (proposedPriceValue != null && double.TryParse(proposedPriceValue.ToString(), out var price))
            {
                proposedPrice = price;
            }


            // Create a PCFItemEntity and populate it
            var pcfItem = new PCFItemEntity
            {
                PCFNumber = pcfNumber,
                ItemNum = itemNum,
                CustNum = custNum.Trim(),
                ItemDesc = itemDesc,
                ProposedPrice = proposedPrice
            };

            // Add the PCFItemEntity to the list
            pcfItems.Add(pcfItem);

            itemNum = string.Empty;
            currentRow++; // Move to the next row
        }

        return pcfItems;
    }






















}

public class ProcessResult
{
    public ProcessStatus Status { get; set; }
    public List<string> Errors { get; set; } = new();

    public int PcfNumber { get; set; }
}
public enum ProcessStatus
{
    Success,
    Failure
}
public static class AppSettings
{
    public static IConfigurationRoot Configuration { get; }
    public static FilePathSettings FilePathSettings { get; }
    public static ConnectionStrings ConnectionStrings { get; }
    static AppSettings()
    {


        Configuration = new
                ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

        FilePathSettings = Configuration
            .GetSection("FilePathSettings")
            .Get<FilePathSettings>();

        ConnectionStrings = Configuration
            .GetSection("ConnectionStrings")
            .Get<ConnectionStrings>();

    }

    public static string GetSetting(string key)
    {
        return Configuration[key];
    }
}
public class FilePathSettings
{
    public string SharedUploadsPath { get; set; }
}
public class ConnectionStrings
{
    public string ChapinData { get; set; }
    public string CiiSQL10ro { get; set; }
    public string CiiSQL10rw { get; set; }
    public string BatPCF { get; set; }
}