using Microsoft.AspNetCore.Components.Forms;

namespace BlazorServerDatagridApp2.Services;

public interface IFileUploadService
{
    Task<bool> UploadFileAsync(IBrowserFile file);
}



public class FileUploadService : IFileUploadService
{
    private readonly string _uploadsPath = Path.Combine("wwwroot", "uploads");

    public FileUploadService()
    {
        if (!Directory.Exists(_uploadsPath))
        {
            Directory.CreateDirectory(_uploadsPath);
        }
    }

    public async Task<bool> UploadFileAsync(IBrowserFile file)
    {
        try
        {
            var filePath = Path.Combine(_uploadsPath, file.Name);
            await using var stream = file.OpenReadStream();
            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fileStream);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
