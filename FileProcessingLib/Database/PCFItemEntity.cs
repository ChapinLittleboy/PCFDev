namespace FileProcessingLib.Database;

public class PCFItemEntity
{
    public string PCFNumber { get; set; }
    public string ItemNum { get; set; }
    public string CustNum { get; set; }
    public string ItemDesc { get; set; }
    public double ProposedPrice { get; set; }
    public int AnnEstUnits { get; set; }
    public int AnnEstDollars { get; set; }
    public decimal LYPrice { get; set; }
    public int LYUnits { get; set; }
    public int ID { get; set; }

    public int PCFHeaderId { get; set; }
    public int PCFNumberInt => int.TryParse(PCFNumber, out var num) ? num : 0; // Handle parsing errors if necessary

    public PCFHeaderEntity PCFHeader { get; set; }


}