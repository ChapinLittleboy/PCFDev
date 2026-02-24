namespace PcfManager.Models;

public class SalesRepEmail
{
    public string RepCode { get; set; } = string.Empty; // 3–4 chars
    public string SalesRegion { get; set; } = string.Empty; // '' or 'ALL' or region
    public string? AgencyName { get; set; }
    public string? EmailList { get; set; }
}