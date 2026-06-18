# Code Cleanup Review

This review is a cleanup backlog for bringing the codebase closer to professional production quality.

No application code was changed as part of this pass. This file only records recommended changes.

## Highest Priority

1. Remove secrets and rotate exposed credentials.
   Files:
   - `PcfManager/Program.cs` line 23 contains a hardcoded Syncfusion license key.
   - `PcfManager/appsettings.json` lines 11-14 contain database credentials.
   - `PcfManager/appsettings.Development.json` lines 9-12 contain database credentials.
   - `PcfManager/appsettings.Production.json` lines 10-13 contain database credentials.
   - `FileProcessingLib/appsettings.json` lines 9-18 contain database credentials.
   - `FileProcessingLib/ExcelToPcfProcessor.cs` lines 25-26 contain hardcoded connection strings.
   Recommended cleanup:
   - Move all secrets to User Secrets, environment variables, or a secure vault.
   - Rotate any credentials that have been committed.
   - Add configuration validation at startup so missing secrets fail fast with a clear error.

2. Lock down file download endpoints and remove unsafe direct file access.
   Files:
   - `PcfManager/Controllers/FileDownloadController.cs` lines 9-19
   - `PcfManager/Shared/FileController.cs` lines 9-20
   - `PcfManager/Pages/PCFEditor.razor` line 1592
   Why this matters:
   - Both endpoints accept a raw `filePath` query parameter and read from the server filesystem directly.
   - There are two controllers solving the same problem in different ways.
   Recommended cleanup:
   - Replace raw path downloads with an allowlisted file token or ID-based lookup.
   - Restrict downloads to a known root folder.
   - Delete the duplicate controller after choosing one supported approach.

3. Clean up the repository history and layout so source code, generated files, and abandoned files are separated.
   Files/folders:
   - `PcfManager/GeneratedPriceBooks/`
   - `PcfManager/wwwroot/pricebooks/`
   - `PcfManager/OldPages/`
   - `PcfManager/Services/ExcelGenerator - Original.cs`
   - `PcfManager/Pages/PCFs.razor.txt`
   - `PcfManager/Pages/PriceIncreaseTool.razor.txt`
   - `BlazorServerDatagridApp2.csprojNOTUSED`
   - `BlazorServerDatagridApp2.csproj.userNOTUSED`
   Why this matters:
   - The repo currently mixes live code with backups, generated spreadsheets, and archived UI pages.
   - That makes maintenance, reviews, and onboarding harder than necessary.
   Recommended cleanup:
   - Remove obsolete files from source control or move them to a clearly named archive folder outside the active app.
   - Keep templates only if they are real runtime assets.
   - Add ignore rules for generated price books, uploads, and logs.

## Architecture And Maintainability

4. Break up the largest files into smaller, testable units.
   Large files identified:
   - `PcfManager/Services/DataService.cs` is about 2,070 lines.
   - `PcfManager/Pages/PCFEditor.razor` is about 1,957 lines.
   - `PcfManager/Pages/PcfDetailReport.razor` is about 1,437 lines.
   - `PcfManager/Pages/PIT.razor` is about 734 lines.
   - `PcfManager/Services/ExcelGenerator.cs` is about 601 lines.
   Recommended cleanup:
   - Split `DataService` by domain area such as PCFs, customers, price books, audit, and reporting.
   - Move `PCFEditor` and `PcfDetailReport` into smaller child components plus a real code-behind/view-model layer.
   - Keep pages focused on presentation and orchestration, not data access and business rules.

5. Move logic out of Razor markup and use the existing code-behind pattern properly.
   Files:
   - `PcfManager/Pages/PCFEditor.razor`
   - `PcfManager/Pages/PCFEditor.razor.cs`
   Why this matters:
   - `PCFEditor.razor.cs` exists but is effectively empty while nearly all logic lives inside the `.razor` file.
   - The page has many injected services, many private fields, and several event handlers all in one file.
   Recommended cleanup:
   - Move state, event handlers, data loading, and helper methods into the partial class.
   - Extract repeated dialogs and form sections into reusable components.
   - Move inline styles into component CSS files.

6. Replace weakly typed data access patterns with typed contracts and safer queries.
   Files:
   - `PcfManager/Services/DataService.cs`
   Specific issues:
   - Uses `dynamic` and `ExpandoObject` heavily.
   - Uses `SELECT *` in multiple places.
   - Contains placeholder or abandoned methods such as `GetHeaderDtoNorepAsyncxxx`, `GetPCFHeaderAsyncXX`, and `MarkItemsForDeletionAsyncWithEditNotes //Not used`.
   - Writes errors to `Console.WriteLine` and often returns empty results instead of surfacing failures cleanly.
   Recommended cleanup:
   - Introduce typed query DTOs and repository/service boundaries.
   - Replace `SELECT *` with explicit column lists.
   - Remove dead methods and rename unclear APIs.
   - Use structured logging and domain-appropriate exception handling.

7. Refactor startup and dependency registration out of `Program.cs`.
   File:
   - `PcfManager/Program.cs`
   Specific issues:
   - Service registration, logging, auth, DB setup, and sample endpoints are all mixed together.
   - A hardcoded `MapPost("/pricebook/generate")` sample endpoint uses a local file path.
   - The catch-all middleware at lines 184-197 is placed after endpoint mapping and is not a clean production error strategy.
   Recommended cleanup:
   - Move service registration into extension methods such as `AddDataAccess`, `AddApplicationServices`, and `AddAuthentication`.
   - Remove demo/test endpoints from production startup.
   - Use the standard exception handling pipeline consistently.

8. Stop suppressing warnings that should be fixed.
   File:
   - `PcfManager/PcfManager.csproj` lines 13-26
   Why this matters:
   - The project suppresses warnings for async misuse, obsolete APIs, unawaited calls, unused fields, duplicate usings, and nullability mismatches.
   Recommended cleanup:
   - Reduce `NoWarn` to only truly intentional exceptions.
   - Fix the underlying warnings, especially `CS4014`, `CS1998`, and nullability issues.
   - Add analyzers and treat important warnings as build failures in CI.

9. Consolidate duplicated infrastructure and clarify ownership between projects.
   Files:
   - `PcfManager/Data/DbConnectionFactory.cs`
   - `FileProcessingLib/Database/DbConnectionFactory.cs`
   - `PcfManager/Data/ApplicationDbContext.cs`
   - `FileProcessingLib/Database/ApplicationDbContext.cs`
   Why this matters:
   - The solution contains overlapping infrastructure classes with different patterns and responsibilities.
   Recommended cleanup:
   - Decide which project owns shared database abstractions.
   - Move shared contracts into a dedicated shared library if needed.
   - Avoid duplicating connection and persistence logic across projects.

## Testing, Naming, And Professional Polish

10. Repair the test setup so tests are meaningful and part of normal development.
    Files:
    - `PCFDev.sln` lines 6-10
    - `PCFProject.tests/PCFProject.tests.csproj`
    - `PCFTestProject/PCFTestProject.csproj`
    - `PCFProject.tests/PCFStartDateValidationTests.cs`
    - `PCFTestProject/UnitTest1.cs`
    Why this matters:
    - The solution does not include either test project.
    - One test project contains only the default placeholder test.
    - The custom test file defines its own local DTOs, validator, and interface instead of testing application code directly.
    Recommended cleanup:
    - Add the real test project to the solution.
    - Delete placeholder tests.
    - Test actual validators/services from the application assembly.
    - Add coverage around the large service and page workflows before refactoring them.

11. Standardize naming and remove legacy/debug naming artifacts.
    Files:
    - `PcfManager/Shared/FileController.cs` line 3 uses namespace `PcfProject.Shared`.
    - `PcfManager/Services/DataService.cs` contains methods named `GetHeaderDtoNorepAsyncxxx` and `GetPCFHeaderAsyncXX`.
    - `PcfManager/Models/pcfCustItemSdateApproved.cs` uses non-standard type casing.
    Recommended cleanup:
    - Align namespaces with the actual project name.
    - Rename placeholder/debug-style methods and types to clear production names.
    - Apply consistent PascalCase naming across all public types and members.

12. Review seeded data and environment-specific behavior for production suitability.
    File:
    - `PcfManager/Data/ApplicationDbContext.cs`
    Specific issues:
    - Contains seeded users and roles directly in the main app context.
    - Enables sensitive data logging outside production.
    Recommended cleanup:
    - Confirm which data should be seeded permanently versus loaded through migrations or admin setup scripts.
    - Ensure no sample or personal accounts are seeded unintentionally.
    - Review logging settings for least exposure.

13. Remove inline styling and presentation duplication from pages.
    Files:
    - `PcfManager/Pages/PCFEditor.razor`
    - Other large `.razor` pages under `PcfManager/Pages/`
    Why this matters:
    - The pages contain many repeated inline `style=` attributes, which makes layout changes harder and less consistent.
    Recommended cleanup:
    - Move styles into `.razor.css` files or shared CSS classes.
    - Standardize common layout patterns for dialogs, forms, and grids.

14. Add lightweight engineering guardrails so the cleanup stays clean.
    Recommended cleanup:
    - Add a formatting and analyzer pass to CI.
    - Add a clear README for build/run/test expectations.
    - Document which folders are source, runtime storage, templates, generated output, and archives.
    - Add a short architectural note describing when to use Dapper versus EF Core.

## Suggested Cleanup Order

1. Secrets and unsafe file download paths
2. Repo cleanup of dead/generated files
3. Test project repair
4. `Program.cs` and warning suppression cleanup
5. `DataService` split
6. `PCFEditor` and report page decomposition
7. Naming, styling, and documentation polish

