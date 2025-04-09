namespace BlazorServerDatagridApp2.Data;

public class User
{
    public int UserId { get; set; }
    public string DomainId { get; set; } // For domain-based authentication
    public string Username { get; set; }
    public string? Password { get; set; } // Optional, for Sales Reps
    public int RoleId { get; set; }
    public string? SalesManagerInitials { get; set; }  // The initials of this user that will be set as Salesmanager in the customer_mst table.

    public Role Role { get; set; }
    public ICollection<UserCustomerAccess> CustomerAccesses { get; set; }
    public ICollection<UserHierarchy> ManagedUsers { get; set; }
    public ICollection<UserHierarchy> ReportsTo { get; set; }
}
