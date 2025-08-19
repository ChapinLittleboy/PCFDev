namespace PcfManager.Models;

public interface ITemplateProvider
{
    Task<IReadOnlyList<TemplateItem>> GetTemplatesAsync(
        bool useWebRoot,
        string subfolder,
        string searchPattern = "*.xlsx");
}
