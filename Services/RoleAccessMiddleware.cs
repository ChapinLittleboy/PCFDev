namespace BlazorServerDatagridApp2.Services;

public class RoleAccessMiddleware
{
    private readonly RequestDelegate _next;
    //private readonly IUserService _userService;
    private readonly IServiceProvider _serviceProvider;

    public RoleAccessMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;

        // Ensure the user is authenticated
        if (!context.User.Identity.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Resolve the scoped IUserService from the service provider
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        // Get the user's role
        var userRole = userService.UserRole;

        // Example: Restrict access to roles other than Administrator
        if (context.Request.Path.StartsWithSegments("/admin") && userRole != "Administrator")
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        var accessibleCustomers = userService.GetAccessibleCustomers(user.Identity.Name);

        // Store role and accessible customers in HttpContext.Items
        context.Items["UserRole"] = userRole;
        context.Items["AccessibleCustomers"] = accessibleCustomers;

        // Allow the request to continue if the role check passes
        await _next(context);
    }

    public async Task InvokeAsync2(HttpContext context)

    {    // Resolve the scoped IUserService from the service provider
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var user = context.User;
        if (user.Identity.IsAuthenticated)
        {
            // Fetch role and accessible customers from UserService or database
            var role = userService.UserRole;
            var accessibleCustomers = userService.GetAccessibleCustomers(user.Identity.Name);

            // Store role and accessible customers in HttpContext.Items
            context.Items["UserRole"] = role;
            context.Items["AccessibleCustomers"] = accessibleCustomers;
        }

        await _next(context);
    }
}
