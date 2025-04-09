using Syncfusion.XlsIO;

namespace FileProcessingLib.Excel;

public class ExcelProcessor
{
    public void ProcessExcelFile(string filePath)
    {
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
                var cellValue = worksheet.Range["A1"].Text;
                Console.WriteLine($"Value in A1: {cellValue}");

                // Close the workbook
                workbook.Close();
            }
        }
    }


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

        if (!DateTime.TryParse(headerSheet.Range["D7"].Text, out startDateTime))
        {
            Console.WriteLine("Invalid start date in cell D7.");
            result = false;
        }

        if (!DateTime.TryParse(headerSheet.Range["D8"].Text, out endDateTime))
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

    // Example helper validation methods
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
}