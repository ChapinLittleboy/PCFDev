using Microsoft.AspNetCore.Mvc;

namespace BlazorServerDatagridApp2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileDownloadController : ControllerBase
{
    [HttpGet("Download")]
    public IActionResult Download([FromQuery] string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
        {
            return NotFound("File not found.");
        }

        var fileName = Path.GetFileName(filePath);
        var mimeType = "application/octet-stream"; // Generic MIME type
        return PhysicalFile(filePath, mimeType, fileName);
    }
}