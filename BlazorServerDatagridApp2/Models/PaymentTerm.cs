namespace BlazorServerDatagridApp2.Models;

public class PaymentTerm
{
    public required string Terms_Code { get; set; }
    public required string Description { get; set; }
    public int Uf_BillingTermActive { get; set; }

}
