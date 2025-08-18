using Microsoft.AspNetCore.Mvc;

namespace PcfProject.Shared;

[Route("api/files")]
[ApiController]
public class FileController : ControllerBase
{
    [HttpGet("download")]
    public IActionResult DownloadFile([FromQuery] string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return NotFound("File not found.");
        }

        var fileName = Path.GetFileName(filePath);
        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return File(fileBytes, contentType, fileName);
    }
}
