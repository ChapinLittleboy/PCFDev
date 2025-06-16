namespace BlazorServerDatagridApp2.Models;

public class ItemPriceDto
{
    public string Item { get; set; }               // ip.item
    public string Description { get; set; }        // im.description
 
    public string Family_Code { get; set; }         // im.family_code
    public string? Family_Code_Description { get; set; }
    public int PrivateLabel { get; set; }         // isnull(im.Uf_PrivateLabel,0)

    public DateTime EffectDate { get; set; }       // ip.effect_date

    public decimal ListPrice { get; set; }         // ip.unit_price1
    public decimal PP1Price { get; set; }          // ip.unit_price2
    public decimal PP2Price { get; set; }          // ip.unit_price3
    public decimal BM1Price { get; set; }          // ip.unit_price4
    public decimal BM2Price { get; set; }          // ip.unit_price5
    public decimal FOBPrice { get; set; }          // ip.unit_price6


    public decimal BookPrice => PP1Price;

    public string? ItemStatus { get; set; }



    // Not mapped to DB; for display only  (Price Increase Tool)
    public decimal NewListPrice { get; set; }
    public decimal NewPP1Price { get; set; }
    public decimal NewPP2Price { get; set; }
    public decimal NewBM1Price { get; set; }
    public decimal NewBM2Price { get; set; }
    public decimal NewFOBPrice { get; set; }


    public string ItemStatusText
    {
        get
        {
            return ItemStatus switch
            {
                "A" => "Active",
                "O" => "Obsolete",
                "S" => "Slow Moving",
                _ => "Unknown"
            };
        }
    }
    public string PrivateLabelText =>
        PrivateLabel == 1 ? "Yes" :
        PrivateLabel == 0 ? "No" :
        "Unknown";
}



// NOTE: This is very similar to the ItemBookPrice class.  Maybe only one is needed?  DECIMAL is better than double for financial calculations, so this is the one to keep.





