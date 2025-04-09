namespace BlazorServerDatagridApp2.Models;

public class PCFItemEntity
{
    public required string PCFNumber { get; set; }
    public string? ItemNum { get; set; }
    public string? CustNum { get; set; }
    public string? ItemDesc { get; set; }
    public double ProposedPrice { get; set; }
    public int AnnEstUnits { get; set; }
    public int AnnEstDollars { get; set; }
    public decimal LYPrice { get; set; }
    public int LYUnits { get; set; }
    public int ID { get; set; }

    // Navigation property for header
    public PCFHeaderDTO? Header { get; set; }
}