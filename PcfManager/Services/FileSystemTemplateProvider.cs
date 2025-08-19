namespace PcfManager.Services;

// FileSystemTemplateProvider.cs
using Microsoft.AspNetCore.Hosting;
using PcfManager.Models;

public sealed class FileSystemTemplateProvider : ITemplateProvider
{
    private readonly IWebHostEnvironment _env;
    public FileSystemTemplateProvider(IWebHostEnvironment env) => _env = env;

    public Task<IReadOnlyList<TemplateItem>> GetTemplatesAsync(bool useWebRoot, string subfolder, string searchPattern = "*.xlsx")
    {
        // Choose root: wwwroot (public) or content root (private)
        var root = useWebRoot ? _env.WebRootPath : _env.ContentRootPath;
        var dir = Path.Combine(root, subfolder);

        if (!Directory.Exists(dir))
            return Task.FromResult<IReadOnlyList<TemplateItem>>(Array.Empty<TemplateItem>());

        var list = Directory.EnumerateFiles(dir, searchPattern, SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName)
            .Select(p => new TemplateItem
            {
                Name = Path.GetFileName(p),
                Path = p // private: store full path
            })
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<TemplateItem>>(list);
    }
}
