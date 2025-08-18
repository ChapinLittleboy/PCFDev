namespace PcfManager.Data;

public class UserHierarchy
{
    public int ManagerId { get; set; }
    public int SubordinateId { get; set; }

    public User Manager { get; set; }
    public User Subordinate { get; set; }
}

public class UserHierarchyDTO
{
    public string ManagerDomainId { get; set; }        // Manager's DomainId
    public string SubordinateDomainId { get; set; }   // Subordinate's DomainId
}