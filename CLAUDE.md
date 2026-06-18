# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
dotnet build PCFDev.sln          # Build entire solution
dotnet build PcfManager          # Build main web app only
dotnet build Chapin.PriceBook    # Build price book library only
dotnet run --project PcfManager  # Run locally (Kestrel)
```

## Project Structure

### PcfManager (Main Blazor Server App)
- **Primary app** - Blazor Server targeting .NET 8
- UI: Syncfusion.Blazor component library
- Auth: Windows Authentication (Negotiate)
- Key pages under `Pages/` (PCFs, PCFEditor, PriceBook, PriceIncrease*, PcfDetailReport, SalesRepEmailMaintenance)
- Services layer under `Services/` (DataService, CustomerService, PriceCalculationService, ExcelGenerator, SalesRepEmailService, etc.)
- Data layer under `Data/` (DbConnectionFactory, DapperExtensions, RepRepository, ApplicationDbContext)
- Models under `Models/` (PCFHeaderEntity, PCFItemEntity, Customer, Rep, etc.)

### Chapin.PriceBook (Class Library)
- Price book generation logic using Syncfusion.XlsIO
- Shared between PcfManager and CLI tools

### FileProcessingLib (Class Library)
- File processing utilities; references Chapin.PriceBook

## Architecture Notes

### Database Access
- Uses **Dapper** for raw SQL queries via `DbConnectionFactory`
- Separate read-write (`CiiSQL01`) and read-only (`CiiSQL10ro`) connection strings
- Read-only connection maps database prefixes (BAT → BAT_App, HEA → HEAT_App) for **Infor SyteLine** ERP
- EF Core `ApplicationDbContext` also registered for ORM-based queries

### PCF Domain
- PCF = Price Change Form
- Core entities: `PCFHeaderEntity` (header-level), `PCFItemEntity` (line items), `PCFHeaderDTO` / `PCFItemDTO` (transfer objects)
- Status workflow managed via `PCFStatus`
- Auditing via Audit.NET + Audit.NET.SqlServer

### Validation
- FluentValidation with `Blazored.FluentValidation`
- Validators in same assemblies as models (e.g., `PCFHeaderDTOValidator`)

### Logging
- Serilog with file sink (`Logs/blazor-log-.txt`, rolling daily, 7-day retention)
- Also writes to Debug and Console

### Key Services (DI lifetime)
- `DbConnectionFactory` - **Singleton**
- `DataService`, `CustomerService` - **Scoped**
- `PriceCalculationService` - **Scoped**
- `UserService` - **Scoped**

### IDataSource / PriceBookDraftService
- Custom abstraction over database connections (`IDataSource` for read-only, `IPriceBookDraftService` for read-write)
- Both registered as Singletons in `Program.cs`

## Configuration
- User secrets ID: `533eb068-cb82-4d3b-a4dc-23904911eab8`
- Connection strings expected: `CiiSQL01`, `CiiSQL10ro`
- Syncfusion licensing registered in `Program.cs` line 23

## Syncfusion License
License key is hardcoded in `Program.cs` (v27 Blazor). If Syncfusion components fail to render, check/renew the license key.
