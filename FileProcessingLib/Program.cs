namespace FileProcessingLib;
internal class Program
{
    static void Main(string[] args)
    {
        var processor = new ExcelToPcfProcessor();

        string excelFilePath = @"C:\SharedUploads\GUL_30402_PcfTemplate_20241126.xlsm";

        try
        {
            processor.ProcessExcelFile(excelFilePath);
            Console.WriteLine("File processed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        Console.WriteLine("Processing complete. Press Enter to exit...");
        Console.ReadLine();

    }
}