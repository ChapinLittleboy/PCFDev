namespace PcfManager.Models;

public class SalesProgram
{
    public string AllowanceType { get; set; } = string.Empty;
    public string PercentageOrAmount { get; set; } = String.Empty;
    public string Uf_ProgNotes { get; set; } = string.Empty;
    public string Uf_ProgOtherNotes { get; set; } = string.Empty;
    public string Uf_GrossNet { get; set; } = string.Empty;
    public string Uf_FixVar { get; set; } = string.Empty;
    public string Uf_PolicyA { get; set; } = string.Empty;
    public string Uf_ProgTiers { get; set; } = string.Empty;
}
