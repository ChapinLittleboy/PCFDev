namespace PcfManager.Models;

public sealed class CustomerItemFiscalSummary
{
    public string Item { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;

    public int CurrentFiscalYear { get; set; }  // e.g., 2025
    public decimal FY_Current_Rev { get; set; }
    public decimal FY_Prior_Rev { get; set; }
    public decimal FY_Prior2_Rev { get; set; }
    public int FY_Current_Qty { get; set; }
    public int FY_Prior_Qty { get; set; }
    public int FY_Prior2_Qty { get; set; }
}