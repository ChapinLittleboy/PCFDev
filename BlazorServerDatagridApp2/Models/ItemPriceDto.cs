namespace BlazorServerDatagridApp2.Models;

public class ItemPriceDto
{
    public string Item { get; set; }               // ip.item
    public string Description { get; set; }        // im.description
    public string FamilyCode { get; set; }         // im.family_code
    public string? Family_Code_Description { get; set; }
    public bool PrivateLabel { get; set; }         // isnull(im.Uf_PrivateLabel,0)

    public DateTime EffectDate { get; set; }       // ip.effect_date

    public decimal ListPrice { get; set; }         // ip.unit_price1
    public decimal PP1Price { get; set; }          // ip.unit_price2
    public decimal PP2Price { get; set; }          // ip.unit_price3
    public decimal BM1Price { get; set; }          // ip.unit_price4
    public decimal BM2Price { get; set; }          // ip.unit_price5
    public decimal FOBPrice { get; set; }          // ip.unit_price6

 

    public decimal BookPrice => PP1Price;

    public string? ItemStatus { get; set; }
}



// NOTE: This is very similar to the ItemBookPrice class.  Maybe only one is needed?  DECIMAL is better than double for financial calculations, so this is the one to keep.





