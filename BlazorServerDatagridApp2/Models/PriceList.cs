namespace BlazorServerDatagridApp2.Models;

public class PriceList
{
    public string Id { get; set; }              
    public string Name { get; set; }
    public List<PriceListEntry> Entries { get; set; } = new();
}

public class PriceListEntry
{
    public string Item { get; set; }
    public decimal Price { get; set; }
    public DateTime EffectiveDate { get; set; }
}

