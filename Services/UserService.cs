using BlazorServerDatagridApp2.Data;
using BlazorServerDatagridApp2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;



namespace BlazorServerDatagridApp2.Services;

public class UserService : IUserService
{
    public int RepID { get; private set; }
    private readonly RepRepository _repRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DatabaseSettings _databaseSettings;
    private User _currentUser;
    private readonly ILogger<UserService> _logger;

    public string CurrentPCFDatabaseName { get; private set; }
    public string CurrentSytelineDatabaseName { get; private set; }

    public Rep CurrentRep { get; private set; }
    public string DomainUserName { get; private set; }
    public string UserName { get; private set; }
    public int? UserId { get; private set; }   // the userId in the Users table used for access control
    public string UserRole { get; private set; }

    public User CurrentUser
    {
        get => _currentUser;
        private set
        {
            if (CurrentUser == null)
            {
                _currentUser = value;
                _isUserSet = true;
            }
        }
    }

    private bool _isUserSet;

    private readonly ApplicationDbContext _context;

    public UserService(
        RepRepository repRepository,
        IHttpContextAccessor httpContextAccessor,
        IOptions<DatabaseSettings> databaseSettings,
        ApplicationDbContext context,
        ILogger<UserService> logger)
    {
        _repRepository = repRepository;
        _httpContextAccessor = httpContextAccessor;
        _databaseSettings = databaseSettings.Value;
        _context = context;
        _logger = logger;

        try
        {
            // Initialize user synchronously to ensure it's ready when needed
            InitializeUserSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing user in UserService constructor");
            // Set default values to prevent null reference exceptions
            UserName = "Unknown";
            UserRole = "Employee";
        }


        // if (string.IsNullOrEmpty(DomainUserName))
        // {
        //     InitializeUserAsync();
        // }
        //_isUserSet = true;
        //UserRole = GetUserRole();

    }

    private void InitializeUserSync()
    {
        // Ensure HttpContext is available before using it
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null || httpContext.User?.Identity?.IsAuthenticated == false)
        {
            _logger.LogWarning("HttpContext is not available during UserService initialization");
            return;
        }

        // Get Windows username
        DomainUserName = httpContext.User.Identity.Name;
        _logger.LogInformation($"Initializing user with domain name: {DomainUserName}");

        if (!string.IsNullOrEmpty(DomainUserName))
        {
            UserName = DomainUserName.Contains('\\') ? DomainUserName.Split('\\').Last() : DomainUserName;
            _logger.LogInformation($"Windows Auth detected: {DomainUserName}, Username: {UserName}");

            try
            {
                CurrentRep = _repRepository.GetRepByUsername(UserName);
                _logger.LogInformation($"Rep found: {CurrentRep?.Name ?? "None"}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rep by username");
            }

            try
            {
                var user = _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.DomainId == UserName);

                UserRole = user?.Role?.RoleName ?? "Executive";
                UserId = user?.UserId ?? 0;
                _logger.LogInformation($"User role set to: {UserRole}, UserId: {UserId}");

                if (CurrentUser == null)
                {
                    CurrentUser = user ?? new User
                    {
                        UserId = 0,
                        DomainId = DomainUserName,
                        Username = UserName,
                        RoleId = 7,
                        Role = new Role { RoleId = 7, RoleName = "Employee" }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user from database");
                // Set default values
                UserRole = "Employee";
                UserId = 0;
            }
        }
        else
        {
            _logger.LogWarning("No Windows username detected during initialization");
        }
    }

    public void SetDatabaseNames(string selection)
    {
        CurrentPCFDatabaseName = selection switch
        {
            "Chapin" => _databaseSettings.PCFDB,
            "Heath" => _databaseSettings.PCFDBHeath,
            _ => throw new ArgumentException("Invalid selection", nameof(selection))
        };
        CurrentSytelineDatabaseName = selection switch
        {
            "Chapin" => _databaseSettings.Syteline,
            "Heath" => _databaseSettings.SytelineHeath,
            _ => throw new ArgumentException("Invalid selection", nameof(selection))
        };
    }

    public async Task InitializeUserAsyncxx()
    {
        if (string.IsNullOrEmpty(DomainUserName))
        {
            //DomainUserName = GetCurrentUserFromWindowsAuth();
            DomainUserName = _httpContextAccessor.HttpContext?.User.Identity?.Name;

            if (!string.IsNullOrEmpty(DomainUserName))
            {
                UserName = DomainUserName?.Contains('\\') == true
                    ? DomainUserName.Split('\\').Last()
                    : DomainUserName;

                Console.WriteLine($"Windows Auth detected: {DomainUserName}");
                CurrentRep = _repRepository.GetRepByUsername(UserName);

                // CurrentUser = await _context.Users
                //    .Include(u => u.CustomerAccesses)
                //    .ThenInclude(uca => uca.Customer)
                //    .FirstOrDefaultAsync(u => u.DomainId == UserName);
            }
            else if (RepID > 0)
            {
                Console.WriteLine("Fallback to session-based rep initialization");
                SetCurrentRep();
            }

            if (!string.IsNullOrEmpty(UserName))
            {
                // UserId = await GetUserIdAsync(UserName);
                // UserRole = await GetUserRoleAsync(UserName);
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.DomainId == UserName);
                UserRole = user?.Role?.RoleName ??
                           "Executive"; // was Unauthorized.  changed to Executive 1/23/2025 to allow everyone in
                UserId = user?.UserId ?? 0;
                if (CurrentUser == null)
                {
                    CurrentUser = user ?? new User
                    {
                        UserId = 0,
                        DomainId = DomainUserName,
                        Username = UserName,
                        RoleId = 7,
                        Role = new Role { RoleId = 7, RoleName = "Employee" }
                    };
                }
            }
        }
    }


    public async Task InitializeUserAsync()
    {
        if (!string.IsNullOrEmpty(DomainUserName))
        {
            _logger.LogInformation("User already initialized, skipping");
            return; // Avoid redundant calls
        }

        // Ensure HttpContext is available before using it
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null || httpContext.User?.Identity?.IsAuthenticated == false)
        {
            _logger.LogWarning("HttpContext is not available.");
            return;
        }

        // Get Windows username
        DomainUserName = httpContext.User.Identity.Name;
        _logger.LogInformation($"Async initializing user with domain name: {DomainUserName}");

        if (!string.IsNullOrEmpty(DomainUserName))
        {
            UserName = DomainUserName.Contains('\\') ? DomainUserName.Split('\\').Last() : DomainUserName;
            _logger.LogInformation($"Windows Auth detected: {DomainUserName}");

            try
            {
                CurrentRep = _repRepository.GetRepByUsername(UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rep by username");
            }

            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.DomainId == UserName);

                UserRole = user?.Role?.RoleName ?? "Executive";
                UserId = user?.UserId ?? 0;

                if (CurrentUser == null)
                {
                    CurrentUser = user ?? new User
                    {
                        UserId = 0,
                        DomainId = DomainUserName,
                        Username = UserName,
                        RoleId = 7,
                        Role = new Role { RoleId = 7, RoleName = "Employee" }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user from database");
                // Set default values
                UserRole = "Employee";
                UserId = 0;
            }
        }
        else
        {
            _logger.LogWarning("No Windows username detected.");
        }
    }




    public string GetCurrentUserFromWindowsAuth()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        return user?.Identity?.IsAuthenticated == true ? user.Identity.Name : "Anonymous";
    }

    public void SetRepID(int repID)
    {
        RepID = repID;
        SetCurrentRep();
    }

    private void SetCurrentRep()
    {
        CurrentRep = _repRepository.GetRepById(RepID);
    }

    public Rep GetRepInfo(int repId)
    {
        return _repRepository.GetRepById(repId);
    }

    public Rep GetCurrentRep()
    {
        return CurrentRep;
    }

    public async Task SetRepAsync(Rep rep)
    {
        CurrentRep = rep;
    }

    public async Task InitializeCurrentRepAsync(RepRepository repRepository)
    {
        // Placeholder for async rep initialization if needed
    }

    public async Task ClearRepAsync()
    {
        CurrentRep = null;
    }

    public bool IsUserInRole(string roleName)
    {
        return CurrentUser != null && CurrentUser.Role.RoleName == roleName;
    }



    private string GetUserRole()
    {
        var domainUserName = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (string.IsNullOrEmpty(domainUserName))
        {
            return "Unauthorized";
        }

        var user = _context.Users
            .Include(u => u.Role)
            .FirstOrDefault(u => u.DomainId == domainUserName);

        return user?.Role?.RoleName ?? "Unauthorized";
    }
    public async Task<string> GetUserRoleAsync(string domainUserName)
    {
        if (string.IsNullOrEmpty(domainUserName))
        {
            return "Unauthorized";
        }

        var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.DomainId == UserName);
        return user?.Role?.RoleName ?? "Employee";  // was "Unauthorized"
    }

    public List<int> GetAccessibleCustomers(string domainUserName)
    {
        if (string.IsNullOrEmpty(domainUserName))
        {
            return new List<int>(); // No access for unauthenticated users
        }

        var user = _context.Users
            .Include(u => u.CustomerAccesses) // Assuming User has a navigation property
            .FirstOrDefault(u => u.DomainId == domainUserName);

        if (user != null)
        {
            return user.CustomerAccesses
                .Select(ca => ca.CustSeq) // Replace with appropriate type if not int
                .ToList();
        }

        return new List<int>(); // Return empty list if user is not found
    }




    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users.AsNoTracking().ToListAsync();
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        return await _context.Roles.ToListAsync();
    }



    public async Task AddUserAsync(User user)
    {
        try
        {
            // Ensure no entity with the same primary key is already tracked
            var trackedEntity = _context.ChangeTracker.Entries<User>()
                .FirstOrDefault(e => e.Entity.UserId == user.UserId);

            if (trackedEntity != null)
            {
                _context.Entry(trackedEntity.Entity).State = EntityState.Detached;
            }

            // Ensure no duplicate primary key exists in the database
            var exists = await _context.Users.AnyAsync(u => u.UserId == user.UserId);
            if (exists)
            {
                throw new InvalidOperationException($"A user with UserId {user.UserId} already exists.");
            }

            // Add the new user
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding user: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateUserAsync(User user)
    {
        try
        {
            var existingUser = _context.ChangeTracker.Entries<User>()
                .FirstOrDefault(e => e.Entity.UserId == user.UserId);

            if (existingUser != null)
            {
                _context.Entry(existingUser.Entity).State = EntityState.Detached; // Detach the tracked instance
            }

            // Attach and mark as modified
            _context.Users.Attach(user);
            _context.Entry(user).State = EntityState.Modified;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user: {ex.Message}");
            throw; // Optionally rethrow the exception for higher-level handling
        }
    }

    public async Task DeleteUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }


    public async Task<int?> GetUserIdAsync(string userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            return null; // Return null if no username is provided
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.DomainId == userName);

        return user?.UserId; // Return UserId if found, otherwise null
    }


    public async Task<List<UserHierarchy>> GetAllUserHierarchiesAsync()
    {
        return await _context.UserHierarchies
            .Include(uh => uh.Manager)
            .Include(uh => uh.Subordinate)
            .ToListAsync();
    }

    public async Task<UserHierarchy> GetUserHierarchyAsync(int managerId, int subordinateId)
    {
        return await _context.UserHierarchies
            .FirstOrDefaultAsync(uh => uh.ManagerId == managerId && uh.SubordinateId == subordinateId);
    }

    public async Task AddUserHierarchyAsync(UserHierarchy userHierarchy)
    {
        _context.UserHierarchies.Add(userHierarchy);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserHierarchyAsync(UserHierarchy userHierarchy)
    {
        _context.UserHierarchies.Update(userHierarchy);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserHierarchyAsync(int managerId, int subordinateId)
    {
        var existingHierarchy = await GetUserHierarchyAsync(managerId, subordinateId);
        if (existingHierarchy != null)
        {
            _context.UserHierarchies.Remove(existingHierarchy);
            await _context.SaveChangesAsync();
        }
    }








}



