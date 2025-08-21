namespace PcfManager.Models;

public class ItemBookPrice
{
    public string? Item { get; set; }
    public string? Description { get; set; }
    public double PP1Price { get; set; }
    public double PP2Price { get; set; }
    public double BM1Price { get; set; }
    public double BM2Price { get; set; }
    public double ListPrice { get; set; }
    public double FOBPrice { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string? Family_Code { get; set; }
    public string? Family_Code_Description { get; set; }

    public double BookPrice => PP1Price;
    public double StandardCost { get; set; }  // im.cur_u_cost
    public string? ItemStatus { get; set; }

}
