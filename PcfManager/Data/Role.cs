namespace PcfManager.Data;

public class Role
{
    public Role()
    {
    }

    public Role(int roleId, string roleName)
    {
        RoleId = roleId;
        RoleName = roleName;
    }

    public int RoleId { get; set; }
    public string RoleName { get; set; }

    public ICollection<User>? Users { get; set; }
}

