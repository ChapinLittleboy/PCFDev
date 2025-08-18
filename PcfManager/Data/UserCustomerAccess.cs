namespace PcfManager.Data;
using PcfManager.Models;


public class UserCustomerAccess
{
    public int UserId { get; set; }
    public string CustNum { get; set; }
    public int CustSeq { get; set; }
    public string Site { get; set; }

    public User User { get; set; }
    public ConsolidatedCustomer Customer { get; set; }
}
