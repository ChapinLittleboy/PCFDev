using Audit.Core;
using Blazored.FluentValidation;
using PcfManager.Data;
using PcfManager.Models;
using PcfManager.Services;
using Chapin.PriceBook;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Syncfusion.Blazor;
using System.Data;
using Microsoft.AspNetCore.Components.Server.Circuits;
using PcfManager.Infrastructure;
//using Blazored.LocalStorage;



//Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXZccHVWQ2NZUkd+XkA="); // was working
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NDA5NjAxOUAzMjM3MmUzMDJlMzBaRmNUdG00bFYxSlBQd1hFUUVSVmFJQVkvSEt3V3ZyNDZYNmllYU5OZm9BPQ==");  // v27 Blazor only
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
// Register the handler:
builder.Services.AddSingleton<CircuitHandler, LoggingCircuitHandler>();


//builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
//builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // For console output
builder.Logging.AddDebug();   // For debug output
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DBNames"));
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<PCFPageState>();
builder.Services.AddScoped<ITemplateProvider, FileSystemTemplateProvider>();
//builder.Services.AddBlazoredLocalStorage();

var csro = builder.Configuration.GetConnectionString("CiiSQL10ro")
           ?? builder.Configuration["ConnectionStrings:CiiSQL10ro"]!;
var csrw = builder.Configuration.GetConnectionString("CiiSQL10rw")
           ?? builder.Configuration["ConnectionStrings:CiiSQL10rw"]!;
builder.Services.AddSingleton<IDataSource>(new SqlDataSource(csro));
builder.Services.AddSingleton<IPriceBookDraftService>(sp =>
    new PriceBookDraftService(csrw));
builder.Services.AddSingleton<IPriceBookVersionService>(_ =>
    new PriceBookVersionService(csrw));

// If you add another source: builder.Services.AddSingleton<IDataSource>(new AltDataSource(...));

// Register generator
builder.Services.AddSingleton<IPriceBookGenerator, PriceBookGenerator>();

builder.Services.AddHttpClient("MyAPI", client =>
{
    client.BaseAddress = new Uri("https://api.ChapinPcfManager.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

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


app.MapPost("/pricebook/generate", async (IPriceBookGenerator gen) =>
{
    var req = new PriceBookRequest(
        TemplatePath: "C:/Users/Willit2/Downloads/HDA MSD Price Book Template v2.xlsx",
        SourceKey: "sql",
        ExcludeFuturePrices: true,
        OutputFileName: "MSD Price Book (Generated).xlsx"
    );

    var bytes = await gen.GenerateAsync(req);
    return Results.File(
        bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        req.OutputFileName ?? "PriceBook.xlsx");
});






app.Run();

//Console.WriteLine($"Environment: {environment.EnvironmentName}");
//Console.WriteLine($"PCFDB: {configuration["DBNames:PCFDB"]}");
