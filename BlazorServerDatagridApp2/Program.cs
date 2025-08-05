using Audit.Core;
using Blazored.FluentValidation;
using BlazorServerDatagridApp2.Data;
using BlazorServerDatagridApp2.Models;
using BlazorServerDatagridApp2.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Syncfusion.Blazor;
using System.Data;




//Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXZccHVWQ2NZUkd+XkA="); // was working
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXdedXVURWFZUkZwXUJWYU4=");  // v27
var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsProduction())
{
    builder.WebHost.CaptureStartupErrors(true);
    builder.WebHost.UseSetting("detailedErrors", "true");
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(
        "Logs/blazor-log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,  // Keep 7 days of logs
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.Debug()    // <--- Add this line
    .WriteTo.Console()  // <--- Optional: Also log to terminal/console
    .CreateLogger();

// Add services to the container.
builder.Host.UseSerilog();
// Enable Windows Authentication
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

/*
builder.Services.AddAuthorization(options =>
{
    // Replace "YourDomain\\YourSecurityGroup" with your actual AD group
    options.AddPolicy("RequireSecurityGroup", policy =>
        policy.RequireRole(@"CHAPIN\CMAProcessors"));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/", "RequireSecurityGroup"); // Secure the entire app
});
*/

builder.Services.AddAuthorization(); // No special policy needed

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/"); // Just require authentication
});


//builder.Services.AddServerSideBlazor().AddCircuitOptions(options => options.DetailedErrors = false);
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DisconnectedCircuitMaxRetained = 50;  // default is 10
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(15);  // default is 3 minutes
        options.DetailedErrors = false;
    });

builder.Services.AddSyncfusionBlazor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddTransient<RepRepository>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<DataService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();
builder.Services.AddScoped<IValidator<PCFHeaderDTO>, PCFHeaderDTOValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<PCFHeaderDTOValidator>();
//builder.Services.AddBlazoredFluentValidation();
builder.Services.AddTransient<FluentValidationValidator>();
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("CiiSQL01")));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("CiiSQL01")),
    ServiceLifetime.Transient);

builder.Services.AddScoped<ExportService>();

builder.Services.AddScoped<IPriceCalculationService, PriceCalculationService>();



//builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
//builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // For console output
builder.Logging.AddDebug();   // For debug output
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DBNames"));
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<PCFPageState>();





//builder.Services.AddAuthorization();


//builder.Services.AddScoped<IFileUploadService, FileUploadService>();


// Add configuration and environment
//builder.Services.AddScoped<IConfiguration>(provider => builder.Configuration);

var configuration = builder.Configuration;
var environment = builder.Environment;
var app = builder.Build();

// Configure the HTTP request pipeline.

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error"); // This is the default.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage(); // Only in development.
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


//app.UseHttpsRedirection();




app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapControllers(); // Map API controllers

//app.UseMiddleware<RoleAccessMiddleware>();

app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (Exception ex)
    {
        // Log the error
        Console.WriteLine($"An error occurred: {ex.Message}");

        // Handle the error gracefully
        context.Response.Redirect("/PCFs"); // Redirect to a custom error page
    }
});


app.Run();

//Console.WriteLine($"Environment: {environment.EnvironmentName}");
//Console.WriteLine($"PCFDB: {configuration["DBNames:PCFDB"]}");
