namespace PcfManager.Models;


// Note:  table name is PCItems
public class PCFItemDTO
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

    public double PP1Price { get; set; }
    public double PP2Price { get; set; }
    public double BM1Price { get; set; }
    public double BM2Price { get; set; }
    public double ListPrice { get; set; }
    public double FOBPrice { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? Family_Code { get; set; }
    public string? Family_Code_Description { get; set; }

    public string? UserName { get; set; }  // set and used only in the update query

    public string? ItemStatus { get; set; }
}


