using BlazorServerDatagridApp2.Data;
using BlazorServerDatagridApp2.Models;


namespace BlazorServerDatagridApp2.Services;

public interface IUserService
{
    int RepID { get; }
    string CurrentPCFDatabaseName { get; }
    string CurrentSytelineDatabaseName { get; }
    string DomainUserName { get; }
    string UserName { get; }
    string UserRole { get; }
    Rep CurrentRep { get; }
    int? UserId { get; }
    User CurrentUser { get; }
    bool IsUserInRole(string RoleName);





    void SetDatabaseNames(string selection);

    Task InitializeUserAsync();
    string GetCurrentUserFromWindowsAuth();

    void SetRepID(int repID);
    Rep GetRepInfo(int repId);
    Rep GetCurrentRep();
    Task SetRepAsync(Rep rep);
    Task InitializeCurrentRepAsync(RepRepository repRepository);
    Task ClearRepAsync();


    List<int> GetAccessibleCustomers(string domainUserName);


    Task<List<User>> GetAllUsersAsync();
    Task<List<Role>> GetAllRolesAsync();

    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(int userId);
    Task<int?> GetUserIdAsync(string userName);
    Task<string> GetUserRoleAsync(string domainUserName);

    // UserHierarchy-related methods
    Task<List<UserHierarchy>> GetAllUserHierarchiesAsync();
    Task<UserHierarchy> GetUserHierarchyAsync(int managerId, int subordinateId);
    Task AddUserHierarchyAsync(UserHierarchy userHierarchy);
    Task UpdateUserHierarchyAsync(UserHierarchy userHierarchy);
    Task DeleteUserHierarchyAsync(int managerId, int subordinateId);
}