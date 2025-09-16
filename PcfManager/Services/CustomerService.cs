using Dapper;
using Microsoft.Data.SqlClient;
using PcfManager.Data;
using PcfManager.Models;
using System.Data;

namespace PcfManager.Services;

public class CustomerService
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly IUserService _userService;

    // Constructor injection for database connection or shared services
    public CustomerService(DbConnectionFactory dbConnectionFactory, IUserService userService)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _userService = userService;
    }

    // Add methods here as needed

    public async Task<Customer> GetCustomerInformationForRepCustomerAsync(string selectedCustomer)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

            // Get the RepCode directly from UserService
            var repCode = _userService.CurrentRep.RepCode;

            var sql = @"
           SELECT ca.site_ref, ca.cust_num as CustNum,  ca.name as CustName
, ca.city as BillToCity, ca.state as BillToState, ca.zip as BillToZip
, ca.[addr##1] as BillToAddress1, ca.[addr##2] BillToAddress2
, cu.terms_code as PaymentTerm, cu.slsman as RepCode, cu.stat as [Status]
,terms.description as PaymentTermsDescription
, cu.End_user_type as EUT
from custaddr_mst ca JOIN customer_mst cu on ca.cust_num = cu.cust_num 
left join terms_mst terms on cu.terms_code = terms.terms_code
AND ca.cust_seq = cu.cust_seq AND ca.site_ref = cu.site_ref and ca.cust_seq = 0
            WHERE 
                cu.slsman = @RepCode AND
                ca.cust_num = @CustNum
            ORDER BY ca.name";

            return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { RepCode = repCode, CustNum = selectedCustomer });
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching customer: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return null;
        }
    }

    public async Task<Customer> GetCustomerInformationForCustomerAsync(string selectedCustomer)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

            // This does not check RepCode

            var sql = @"
           SELECT top 1 ca.site_ref, ca.cust_num as CustNum,  ca.name as CustName
, ca.city as BillToCity, ca.state as BillToState, ca.zip as BillToZip
, ca.[addr##1] as BillToAddress1, ca.[addr##2] BillToAddress2
, cu.terms_code as PaymentTerms, terms.Description as PaymentTermsDescription, cu.slsman as RepCode, cu.stat as [Status]
, cu.Uf_PROG_BASIS as PricingMethod, cu.Uf_FrtTerms as FreightTerms, cu.Decifld2 as FreightMinimums, cu.uf_c_slsmgr as SalesManager
, cu.End_user_type as EUT, cu.cust_type as BuyingGroup
, sm.SalesManagerName, sm.SalesManagerEmail, sre.EmailList as SalesRepEmail
from custaddr_mst ca 
JOIN customer_mst cu on ca.cust_num = cu.cust_num 
join terms_mst terms on cu.terms_code = terms.terms_code
AND ca.cust_seq = cu.cust_seq AND ca.site_ref = cu.site_ref and ca.cust_seq = 0
left join Chap_SalesManagers sm on sm.SalesManagerInitials = cu.uf_c_slsmgr
Left join Chap_SalesRepEmail sre on sre.RepCode = cu.slsman

            WHERE 
                dbo.ExpandKyByType('CustNumType',ca.cust_num) = dbo.ExpandKyByType('CustNumType',@CustNum) 
                
            ";

            return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { CustNum = selectedCustomer });
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching customer: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return null;
        }
    }


    public async Task<Customer> GetSalesManagerForCustomerAsync(string selectedCustomer)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

            // This does not check RepCode

            var sql = @"
           SELECT top 1 
cu.slsman as RepCode,  cu.uf_c_slsmgr as SalesManager

from customer_mst cu where cu.cust_seq = 0 and  dbo.ExpandKyByType('CustNumType',cu.cust_num) = dbo.ExpandKyByType('CustNumType',@CustNum) 

            ";

            return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { CustNum = selectedCustomer });
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching customer: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return null;
        }
    }



    public async Task<List<SalesProgram>> GetCustomerProgramsAsync(string selectedCustomer)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

            // This does not check RepCode

            var sql = @"
SELECT 
    
    AllowanceType,
    CASE 
        WHEN Percentage > 0 THEN CAST(Percentage AS VARCHAR) + '%' 
        ELSE '$' + FORMAT(Amount, 'N0') 
    END AS PercentageOrAmount,
    Uf_ProgNotes,
    Uf_ProgOtherNotes,
    Uf_GrossNet,
    Uf_FixVar,
    Uf_PolicyA,
    CONCAT(
        CASE WHEN Uf_ProgTier1 > 0 THEN CONCAT(CAST(Uf_ProgTier1 AS VARCHAR), ' (Tier1) ') ELSE '' END,
        CASE WHEN Uf_ProgTier2 > 0 THEN CONCAT(CAST(Uf_ProgTier2 AS VARCHAR), ' (Tier2) ') ELSE '' END,
        CASE WHEN Uf_ProgTier3 > 0 THEN CONCAT(CAST(Uf_ProgTier3 AS VARCHAR), ' (Tier3) ') ELSE '' END,
        CASE WHEN Uf_ProgTier4 > 0 THEN CONCAT(CAST(Uf_ProgTier4 AS VARCHAR), ' (Tier4) ') ELSE '' END
    ) AS Uf_ProgTiers
FROM 
    custprog_mst
WHERE 
    dbo.ExpandKyByType('CustNumType', CustNum) = dbo.ExpandKyByType('CustNumType',@CustNum)
    AND Uf_ProgArchive = 0
    AND EndDate > GETDATE()                
            ";

            return (await connection.QueryAsync<SalesProgram>(sql, new { CustNum = selectedCustomer })).ToList();
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching SalesPrograms: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return null;
        }
    }

    public Customer GetCustomerInformationForRepCustomer(string selectedCustomer)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

            // Get the RepCode directly from UserService
            var repCode = _userService.CurrentRep.RepCode;

            var sql = @"
            SELECT ca.site_ref, ca.cust_num as CustNum, ca.name as CustName
            , ca.city as BillToCity, ca.state as BillToState, ca.zip as BillToZip
            , ca.[addr##1] as BillToAddress1, ca.[addr##2] as BillToAddress2
            , cu.terms_code as PaymentTerms, cu.slsman as RepCode, cu.Stat as Status
, cu.Uf_PROG_BASIS as PricingMethod
            FROM custaddr_mst ca 
            JOIN customer_mst cu ON ca.cust_num = cu.cust_num 
            AND ca.cust_seq = cu.cust_seq AND ca.site_ref = cu.site_ref AND ca.cust_seq = 0
            WHERE cu.slsman = @RepCode
            AND ca.cust_num = @CustNum
            ORDER BY ca.name";

            return connection.QuerySingleOrDefault<Customer>(sql, new { RepCode = repCode, CustNum = selectedCustomer });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching customer: {ex.Message}");
            return null;
        }
    }

    public async Task<IList<CustomerItemFiscalSummary>> GetCustomerItemFiscalSummaryAsync(
        string custNum, string? item = null, CancellationToken ct = default)
    {
    

        const string sql = @"
;WITH FY AS (
    SELECT 
        fc.FiscalYear       AS FYCurrent,
        fc.FiscalYearStart  AS FYCurrentStart,
        fc.FiscalYearEnd    AS FYCurrentEnd
    FROM Tempwork.dbo.FiscalCalendarVw fc
    WHERE fc.[Date] = CAST(GETDATE() AS date)      -- single row
),
Bounds AS (
    SELECT
        f.FYCurrent,
        MIN(CASE WHEN v.FiscalYear = f.FYCurrent - 1 THEN v.FiscalYearStart END) AS FYPriorStart,
        MAX(CASE WHEN v.FiscalYear = f.FYCurrent - 1 THEN v.FiscalYearEnd   END) AS FYPriorEnd,
        MIN(CASE WHEN v.FiscalYear = f.FYCurrent - 2 THEN v.FiscalYearStart END) AS FYPrior2Start,
        MAX(CASE WHEN v.FiscalYear = f.FYCurrent - 2 THEN v.FiscalYearEnd   END) AS FYPrior2End,
        MIN(CASE WHEN v.FiscalYear = f.FYCurrent - 3 THEN v.FiscalYearStart END) AS FYPrior3Start,
        MAX(CASE WHEN v.FiscalYear = f.FYCurrent - 3 THEN v.FiscalYearEnd   END) AS FYPrior3End,
        MAX(CASE WHEN v.FiscalYear = f.FYCurrent     THEN v.FiscalYearEnd   END) AS FYCurrentEnd
    FROM Tempwork.dbo.FiscalCalendarVw v
    CROSS JOIN FY f
    GROUP BY f.FYCurrent
),
InvoiceData AS (
    -- Bat_App
    SELECT 
        ii.item                                AS Item,
        im.Description                         AS ItemDescription,
        cal.FiscalYear                         AS FiscalYear,
        CAST(ii.qty_invoiced * (ii.price * ((100.0 - ISNULL(ih.disc, 0.0)) / 100.0)) AS decimal(18,2)) AS RevAmount,
        CAST(ii.qty_invoiced AS decimal(18,2)) AS QtyInvoiced
    FROM Bat_App.dbo.inv_item_mst ii WITH (NOLOCK)
    INNER JOIN Bat_App.dbo.inv_hdr_mst ih WITH (NOLOCK)
        ON ii.inv_num = ih.inv_num AND ii.inv_seq = ih.inv_seq
    LEFT JOIN Bat_App.dbo.item_mst im WITH (NOLOCK)
        ON ii.item = im.item
    INNER JOIN Tempwork.dbo.FiscalCalendarVw cal
        ON cal.[Date] = CAST(ih.inv_date AS date)
    CROSS JOIN Bounds b
    WHERE ih.cust_num = @CustNum
      AND CAST(ih.inv_date AS date) BETWEEN b.FYPrior3Start AND b.FYCurrentEnd
      AND (@Item IS NULL OR ii.item = @Item)

    UNION ALL

    -- Kent_App
    SELECT 
        ii.item                                AS Item,
        im.Description                         AS ItemDescription,
        cal.FiscalYear                         AS FiscalYear,
        CAST(ii.qty_invoiced * (ii.price * ((100.0 - ISNULL(ih.disc, 0.0)) / 100.0)) AS decimal(18,2)) AS RevAmount,
        CAST(ii.qty_invoiced AS decimal(18,2)) AS QtyInvoiced
    FROM Kent_App.dbo.inv_item_mst ii WITH (NOLOCK)
    INNER JOIN Kent_App.dbo.inv_hdr_mst ih WITH (NOLOCK)
        ON ii.inv_num = ih.inv_num AND ii.inv_seq = ih.inv_seq
    LEFT JOIN Kent_App.dbo.item_mst im WITH (NOLOCK)
        ON ii.item = im.item
    INNER JOIN Tempwork.dbo.FiscalCalendarVw cal
        ON cal.[Date] = CAST(ih.inv_date AS date)
    CROSS JOIN Bounds b
    WHERE ih.cust_num = @CustNum
      AND CAST(ih.inv_date AS date) BETWEEN b.FYPrior3Start AND b.FYCurrentEnd
      AND (@Item IS NULL OR ii.item = @Item)
),
Agg AS (
    SELECT 
        d.Item,
        MAX(d.ItemDescription) AS ItemDescription,
        SUM(CASE WHEN d.FiscalYear = b.FYCurrent     THEN d.RevAmount   ELSE 0 END) AS FY_Current_Rev,
        SUM(CASE WHEN d.FiscalYear = b.FYCurrent - 1 THEN d.RevAmount   ELSE 0 END) AS FY_Prior_Rev,
        SUM(CASE WHEN d.FiscalYear = b.FYCurrent - 2 THEN d.RevAmount   ELSE 0 END) AS FY_Prior2_Rev,
       SUM(CASE WHEN d.FiscalYear = b.FYCurrent - 3 THEN d.RevAmount   ELSE 0 END) AS FY_Prior3_Rev,

        SUM(CASE WHEN d.FiscalYear = b.FYCurrent     THEN d.QtyInvoiced ELSE 0 END) AS FY_Current_Qty,
        SUM(CASE WHEN d.FiscalYear = b.FYCurrent - 1 THEN d.QtyInvoiced ELSE 0 END) AS FY_Prior_Qty,
        SUM(CASE WHEN d.FiscalYear = b.FYCurrent - 2 THEN d.QtyInvoiced ELSE 0 END) AS FY_Prior2_Qty,
         SUM(CASE WHEN d.FiscalYear = b.FYCurrent - 3 THEN d.QtyInvoiced ELSE 0 END) AS FY_Prior3_Qty
    FROM InvoiceData d
    CROSS JOIN Bounds b
    GROUP BY d.Item
)
SELECT 
    a.Item,
    a.ItemDescription,
    b.FYCurrent AS CurrentFiscalYear,
    a.FY_Current_Rev,
    a.FY_Prior_Rev,
    a.FY_Prior2_Rev,
    a.FY_Prior3_Rev,
    a.FY_Current_Qty,
    a.FY_Prior_Qty,
    a.FY_Prior2_Qty,
    a.FY_Prior3_Qty
FROM Agg a
CROSS JOIN Bounds b
ORDER BY a.Item;
";

        var results = new List<CustomerItemFiscalSummary>();

        using var conn = _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);
        if (conn == null)
            throw new InvalidOperationException("Database connection could not be created.");

        var parameters = new
        {
            CustNum = custNum,
            Item = item // Dapper will pass DBNull for null automatically
        };

        // Use CommandDefinition so we can keep timeout, command type, and support cancellation
        var command = new CommandDefinition(
            commandText: sql,
            parameters: parameters,
            transaction: null,
            commandTimeout: 180,
            commandType: CommandType.Text,
            // Buffered is the default; switch to CommandFlags.None if you need unbuffered streaming
            flags: CommandFlags.Buffered,
            cancellationToken: ct
        );

        // Dapper maps columns -> properties case-insensitively by name
        var rows = await conn.QueryAsync<CustomerItemFiscalSummary>(command).ConfigureAwait(false);

        // If you specifically want a List<T> (not IEnumerable<T>)
        results = rows.AsList();
        return results;
    }


}