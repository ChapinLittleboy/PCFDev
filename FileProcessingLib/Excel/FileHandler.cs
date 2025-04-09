using OfficeOpenXml;

namespace FileProcessingLib;
public class FileHandler
{
    public ExcelPackage workbook;
    public ExcelPackage OpenExcelWorkbook(string filePath)

    {
        // Validate the file path
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File does not exist.");
            return null;
        }

        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            workbook = new ExcelPackage(new FileInfo(filePath));
            return workbook;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening Excel file: {ex.Message}");
            return null;
        }


    }

}
