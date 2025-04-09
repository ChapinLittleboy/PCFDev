namespace FileProcessingLib.Database;

public class PricingRequestDetail
{
    public string ItemCode { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}