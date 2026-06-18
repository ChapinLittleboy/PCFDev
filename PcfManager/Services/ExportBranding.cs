using Syncfusion.Blazor.Grids;
using Syncfusion.XlsIO;

namespace PcfManager.Services;

public static class ExportBranding
{
    public const string DocumentFontName = "Arial";

    public static ExcelExportProperties CreateExcelExportProperties(string fileName)
        => new()
        {
            FileName = fileName,
            Theme = CreateExcelTheme()
        };

    public static void ApplyTo(IApplication application)
    {
        application.StandardFont = DocumentFontName;
    }

    public static void ApplyTo(IWorkbook workbook)
    {
        foreach (IStyle style in workbook.Styles)
        {
            style.Font.FontName = DocumentFontName;
        }

        foreach (IWorksheet worksheet in workbook.Worksheets)
        {
            if (worksheet.UsedRange is { } usedRange)
            {
                usedRange.CellStyle.Font.FontName = DocumentFontName;
            }
        }
    }

    private static ExcelTheme CreateExcelTheme()
    {
        var font = new ExcelStyle { FontName = DocumentFontName };

        return new ExcelTheme
        {
            Header = font,
            Record = font,
            Caption = font
        };
    }
}
