using AutoMapper;
using Dapper;
using Microsoft.Data.SqlClient;
using PcfManager.Data;
using PcfManager.Models;
using Syncfusion.Blazor.RichTextEditor;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using static Dapper.SqlMapper;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;




namespace PcfManager.Services;

public class DataService
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly IMapper _mapper;
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataService> _logger;


    public DataService(DbConnectionFactory dbConnectionFactory, IUserService userService, IMapper mapper,
        IConfiguration configuration, ILogger<DataService> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _userService = userService;
        _mapper = mapper;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<ExpandoObject>> GetDynamicSLDataAsync(string query)
    {
        using var connection = _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

        try
        {
            var result = await connection.QueryAsync<dynamic>(query);
            Console.WriteLine($"Rows retrieved: {result.Count()}");
            var dd = result.ToExpandoObjects();
            // Convert DapperRow to ExpandoObject
            var expandoList = new List<ExpandoObject>();

            foreach (var row in result)
            {
                IDictionary<string, object> dictionary = row as IDictionary<string, object>;
                if (dictionary != null)
                {
                    var expando = new ExpandoObject();
                    var expandoDict = (IDictionary<string, object>)expando;

                    foreach (var kvp in dictionary)
                    {
                        expandoDict[kvp.Key] = kvp.Value;
                    }

                    expandoList.Add(expando);
                }
            }

            Console.WriteLine("Returning data...");
            return expandoList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return new List<ExpandoObject>(); // Return empty list to prevent crashes
        }


    }

    public async Task<List<PCFHeaderDTO>> GetPCFHeadersAsync(string status)
    {
        if (_userService.CurrentUser == null)
        {
            await _userService.InitializeUserAsync();
        }

        string query =

                @"SELECT distinct Upper(SRNum) as RepID, ProgControl.CustNum as CustomerNumber, CustName as CustomerName,
               ProgSDate as StartDate, ProgEDate as EndDate, PCFNum as PcfNumber, PCFStatus as ApprovalStatus
                    ,PcfType as PcfType, cc.Eut as MarketType, BuyingGroup as BuyingGroup, SubmittedBy as SubmittedBy
                        ,cc.Salesman as Salesman, cc.SalesManager as SalesManager
               FROM ProgControl --join UserCustomerAccesses uca on ProgControl.CustNum = uca.CustNum
                left join ConsolidatedCustomers cc on ProgControl.CustNum = cc.CustNum and cc.custseq = 0
                WHERE (1 = 1 AND  progcontrol.CustNum is not null AND progcontrol.ProgSDate is not null)
                AND progcontrol.ProgSDate > '2019-12-31'
               ORDER BY PCFNum DESC"
            ;


        //var parameters = new { CurrentDate = DateTime.Now.Date};

        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        return (await connection.QueryAsync<PCFHeaderDTO>(query)).ToList();
    }


    public async Task<PCFHeaderDTO> GetHeaderDtoNorepAsyncxxx(int pcfNumber)
    {
        var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        if (connection == null)
            throw new InvalidOperationException(
                "Connection is null. Ensure the DB connection factory is set up correctly.");


        var headerEntity = await connection.QuerySingleOrDefaultAsync<PCFHeaderEntity>(
            "SELECT * FROM ProgControl WHERE PcfNum = @PcfNumber",
            new { PcfNumber = pcfNumber });
        if (headerEntity == null)
        {
            Console.WriteLine("No header entity found for the given PcfNumber.");
            return null; // Or handle the case where no result is found.
        }

        Console.WriteLine("have headerEntity");
        return _mapper.Map<PCFHeaderDTO>(headerEntity);
    }

    public async Task<dynamic> GetPreviousPCFDetailsAsync(string custNum, int currentPcfNum)
    {
        // SQL query to retrieve the needed fields
        var sql = @"
        SELECT TOP 1 Buyer, CustContact, Email 
        FROM progcontrol 
        WHERE custnum = @CustNum AND pcfnum < @CurrentPcfNum
        ORDER BY date DESC";

        using var
            connection =
                _dbConnectionFactory.CreateReadWriteConnection(_userService
                    .CurrentPCFDatabaseName); // Replace with your DB connection factory
        var result =
            await connection.QueryFirstOrDefaultAsync<dynamic>(sql,
                new { CustNum = custNum, CurrentPcfNum = currentPcfNum });
        return result;
    }


    public async Task<dynamic> GetBuyerInfoAsync(string custNum)
    {
        // SQL query to retrieve the needed fields
        var sql = @"
        SELECT TOP 1 BuyerName, BuyerEmail, BuyerPhone
        FROM CustomerBuyerInformation 
        WHERE custnum = @CustNum";

        using var
            connection =
                _dbConnectionFactory.CreateReadWriteConnection(_userService
                    .CurrentPCFDatabaseName); // Replace with your DB connection factory
        var result =
            await connection.QueryFirstOrDefaultAsync<dynamic>(sql,
                new { CustNum = custNum });
        return result;
    }

    public async Task<dynamic> GetBuyerInfoFromSytelineAsync(string custNum)
    {
        // SQL query to retrieve the needed fields
        var sql = @"
        SELECT TOP 1 Uf_BuyerName as BuyerName, Uf_BuyerEmail as BuyerEmail, Uf_BuyerPhone as BuyerPhone
        FROM customer_mst 
        WHERE LTRIM(RTRIM(cust_num)) = @CustNum and Cust_seq = 0";

        using var
            connection =
                _dbConnectionFactory.CreateReadOnlyConnection(_userService
                    .CurrentSytelineDatabaseName); // Replace with your DB connection factory
        var result =
            await connection.QueryFirstOrDefaultAsync<dynamic>(sql,
                new { CustNum = custNum });
        return result;
    }

    public async Task<PCFHeaderDTO> GetHeaderDtoNorepAsync(int pcfNumber)
    {
        var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        if (connection == null)
            throw new InvalidOperationException(
                "Connection is null.Ensure the DB connection factory is set up correctly.");

        var headerEntity = new PCFHeaderEntity();
        headerEntity = await connection.QuerySingleOrDefaultAsync<PCFHeaderEntity>(
            "SELECT * FROM ProgControl WHERE PcfNum = @PcfNumber",
            new { PcfNumber = pcfNumber });
        Console.WriteLine(DateTime.MinValue.ToString());
        if (headerEntity == null)
        {
            Console.WriteLine("No header entity found for the given PcfNumber.");
            return null; // Or throw an exception if this is considered an error condition.
        }

        if (_mapper == null)
            throw new InvalidOperationException("Mapper is not initialized.");

        var dto = _mapper.Map<PCFHeaderDTO>(headerEntity);

        // Fetch rep info from linked server
        var repInfo = await connection.QuerySingleOrDefaultAsync<(string RepName, string RepAgency, string RepEmail)>(
            @"SELECT RepCode AS RepName, AgencyName AS RepAgency, EmailList AS RepEmail
          FROM CIISQL10.[Bat_App].[dbo].[Chap_SalesRepEmail] sre
          join CIISQL10.[Bat_App].[dbo].[customer_mst] cu0 on sre.RepCode = cu0.slsman 
          where ltrim(cu0.cust_num) = @CustNum and cu0.cust_seq = 0",
            new { CustNum = headerEntity.CustNum });

        if (repInfo != default)
        {
            dto.RepName = repInfo.RepName;
            dto.RepAgency = repInfo.RepAgency;
            dto.RepEmail = repInfo.RepEmail;
        }

        return dto;
    }


    public async Task<List<PCFItemDTO>> GetItemsDtoNorepAsync(string pcfNumber)
    {
        var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        var itemEntities = await connection.QueryAsync<PCFItemEntity>(
            "SELECT * FROM PCItems WHERE PcfNumber = @PcfNumber",
            new { PcfNumber = pcfNumber });

        return _mapper.Map<List<PCFItemDTO>>(itemEntities);
    }

    public async Task<List<pcfCustItemSdateApproved>> GetPcfCustItemStartApproved(string custnum)
    {
        var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        var pcfdata = await connection.QueryAsync<pcfCustItemSdateApproved>(
            "SELECT pci.PcfNumber as PcfNum,  pch.CustNum, pci.ItemNum, pch.ProgSDate as Sdate FROM " +
            "Progcontrol pch join  PCItems pci on cast(pch.pcfnum as nvarchar) = pci.pcfnumber " +
            "WHERE pch.CustNum = @Custnum and pch.Approved = 3 and pch.ProgEDate > getdate()",
            new { Custnum = custnum.Trim() });

        return pcfdata.ToList();
    }

    public async Task<List<Customer>> GetCustomersForRepAsync()
    {
        try
        {
            using var connection =
                _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

            // Get the RepCode directly from UserService
            // Get the RepCode directly from UserService
            var repCode = _userService?.CurrentRep?.RepCode;

            var sql = @"
            SELECT ca.cust_num AS CustNum, Replace(ca.name,'&', '(and)') AS CustName
            FROM customer_mst cu
            JOIN custaddr_mst ca 
                ON cu.cust_num = ca.cust_num 
                AND cu.cust_seq = ca.cust_seq 
                AND cu.cust_seq = 0
            WHERE 
                ca.name NOT LIKE '%Do Not use%' 
                AND cu.stat = 'A' 
                AND cu.slsman = @RepCode
            ORDER BY ca.name";

            return (await connection.QueryAsync<Customer>(sql, new { RepCode = repCode })).ToList();
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching customers: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return new List<Customer>();
        }
    }

    public async Task<List<PCFHeaderEntity>> GetPCFsForCustNumAsync(string custNum)
    {
        try
        {
            using var connection =
                _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName); // ciisql01

            // Get the RepCode directly from UserService
            var repCode = _userService.CurrentRep.RepCode;

            var sql = @"
            SELECT PcfNum, ProgSDate , ProgEDate
            FROM Progcontrol           
            WHERE 
                SRNum = @RepCode
                and CustNum = @CustNum
            ORDER BY ProgEDate DESC";

            return (await connection.QueryAsync<PCFHeaderEntity>(sql,
                new { RepCode = repCode, CustNum = custNum.Trim() })).ToList();
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching customers: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return new List<PCFHeaderEntity>();
        }
    }

    public async Task<PCFHeaderEntity> GetPCFHeaderAsyncXX(int pcfNumber)
    {
        try
        {
            using var connection =
                _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName); // ciisql01

            var CurrentUserID = _userService.CurrentUser.UserId;

            var sql = @"
            SELECT Progcontrol.*
            FROM Progcontrol join UserCustomerAccesses uca on ProgControl.CustNum = uca.CustNum         
            WHERE  
                PCFNum = @PCFNum AND uca.UserId = @CurrentUserID
            ; ";


            return await connection.QueryFirstOrDefaultAsync<PCFHeaderEntity>(sql, new { PCFNum = pcfNumber });
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching PCFHeader: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return new PCFHeaderEntity();
        }
    }

    public async Task<PCFHeaderEntity> GetPCFHeaderAsync(int pcfNumber)
    {
        int CurrentUserID = 1;

        try
        {
            using var connection =
                _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName); // ciisql01

            if (_userService.CurrentUser != null)
            {
                CurrentUserID = _userService.CurrentUser.UserId;
            }
            else
            {
                _userService.InitializeUserAsync();

            }

            var sql =
                @"SELECT PCFNum, [Date], Warehouse, Dropship, OtherDropship, OtherWarehouse, DWOther, DWOtherText, ProgSDate, 
ProgEDate, p.CustNum, CustName, BTName, BTAddr, BTCity, BTState, BTZip, BTPhone, BTFax, BTFaxPerm, Buyer, RepName, RepEmail, 
RepAgency, RepPhone, OtherTerms, STName, STAddr, STCity, STState, STZip, STPhone, CustContact, Email, BuyingGroup, 
OtherDating, OtherDatingApprvl, FtPickUpAllow, FtDSPPD, FtDSDollars, GenNotes, NSShipNotes, RoutingNotes, AdPercSales, 
AdCMemo, AdDFI, AdPdQuarterly, AdAnnually, AdSemiAnnually, AdMonthly, AdPOP, AdNotes, VolPercSales, VolCMemo, VolDFI, 
VolPaidQuarterly, VolAnnually, VolSemiAnnually, VolMonthly, VolNotes, DefectiveDFI, ActualDefectives, DefectiveIssueCM, 
CoopOtherNotes, SubmittedBy, SubmitDate, SalesMngrApproval, SalesMngrDate, VPSalesApprovl, VPSalesDate, Approved, FtPUAllocPerc, 
SoftwareNotes, SRNum, MngrComments, ErrorMsg, OtherMsg, ItemComments, EditBy, EditDate, EditNotes, cc.EUT, OrigPCF, Tariff, Standard_Terms, 
Standard_Terms_Text, Promo_Terms, Promo_Terms_Text, Standard_Freight_Terms, Freight_Minimums, Other_Freight_Minimums, CmaRef, PCFStatus, 
PcfType, LastUpdatedBy, LastUpdatedDate, BuyerEmail, BuyerPhone, SubmitterEmail 
FROM custinfo.dbo.ProgControl p
Join consolidatedcustomers cc on p.CustNum = cc.CustNum and cc.CustSeq = 0
 WHERE  
            PCFNum = @PCFNum 
        ;";

            var result = await connection.QueryFirstOrDefaultAsync<PCFHeaderEntity>(sql,
                new { PCFNum = pcfNumber, CurrentUserID = CurrentUserID });

            if (result == null)
            {
                // Log unauthorized access attempt
                Console.WriteLine(
                    $"User {CurrentUserID} attempted to access a PCF {pcfNumber} they are not authorized for.");

                // Optionally, you can throw a custom exception
                // throw new UnauthorizedAccessException("You do not have access to view this PCF.");
            }

            return result;
        }


        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching PCFHeader: {ex.Message}");

            // Optionally return an empty object or throw the exception to be handled by the caller
            throw new Exception("An error occurred while fetching the PCF data.");
        }
    }


    public async Task<List<PCFItemEntity>> GetPcfItemsAndPricesAsync(string pcfNumber)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);

            // Get the RepCode directly from UserService
            // var repCode = _userService.CurrentRep.RepCode;   NOT USING REPCODE!

            var sql = @"
            SELECT * from PCItems where PCFNumber = @PCFNumber";

            return (await connection.QueryAsync<PCFItemEntity>(sql, new { PCFNumber = pcfNumber })).ToList();
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching PCF items: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return new List<PCFItemEntity>();
        }
    }

    public List<PCFItemEntity> GetPcfItemsAndPrices(string pcfNumber)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);

            // Get the RepCode directly from UserService
            var repCode = _userService.CurrentRep.RepCode;

            var sql = @"
            SELECT * from PCItems where PCFNumber = @PCFNumber";

            return connection.Query<PCFItemEntity>(sql, new { PCFNumber = pcfNumber }).ToList();
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching PCF items: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return new List<PCFItemEntity>();
        }
    }


    public List<PaymentTerm> GetPaymentTerms()
    {
        try
        {
            using var connection =
                _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

            if (_userService.CurrentUser == null)

            {
                _userService.InitializeUserAsync();

            }

            // Get the RepCode directly from UserService
            var repCode = _userService.CurrentRep.RepCode;

            var sql = @"
            Select Description, Terms_Code, Uf_BillingTermActive from terms_mst  where Uf_BillingTermActive = 1  order by Description";

            return connection.Query<PaymentTerm>(sql).ToList();
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching Payment Terms: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return new List<PaymentTerm>();
        }
    }

    public async Task<List<PaymentTerm>> GetPaymentTermsAsync()
    {
        try
        {
            using var connection =
                _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);
            if (_userService.CurrentUser == null)
            {
                _userService.InitializeUserAsync();

            }

            // Get the RepCode directly from UserService
            var repCode = _userService.CurrentRep.RepCode;

            var sql = @"
            Select Description, Terms_Code, Uf_BillingTermActive from terms_mst  where Uf_BillingTermActive = 1  order by Description";

            return (await connection.QueryAsync<PaymentTerm>(sql)).ToList();
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching Payment Terms: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return new List<PaymentTerm>();
        }
    }


    public async Task<List<ItemBookPrice>> GetItemBookPricesAsync(string? databaseName = null)
    {
        try
        {
            using var connection =
                _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

            // Get the RepCode directly from UserService
            // var repCode = _userService.CurrentRep.RepCode;

            var sql = @"
SELECT 
    item_mst.item, 
    item_mst.description, 
    item_mst.family_code,
    item_mst.cur_u_cost as StandardCost,
    famcode_mst.description as Family_Code_Description,
    latest_price.effect_date AS EffectiveDate, 
    ISNULL(latest_price.unit_price1, 0.0) AS ListPrice,
    ISNULL(latest_price.unit_price2, 0.0) AS PP1Price,  -- 4K Book
    ISNULL(latest_price.unit_price3, 0.0) AS PP2Price,  -- 12.5k Book
    ISNULL(latest_price.unit_price4, 0.0) AS BM1Price,  -- BG Midstates 4k
    ISNULL(latest_price.unit_price5, 0.0) AS BM2Price,  -- BG Midstates 12.5k
    ISNULL(latest_price.unit_price6, 0.0) AS FobPrice
    ,item_mst.stat as ItemStatus
FROM 
    item_mst
LEFT JOIN 
    famcode_mst 
    ON item_mst.family_code = famcode_mst.family_code
OUTER APPLY (
    SELECT TOP 1 
        effect_date,
        unit_price1,
        unit_price2,
        unit_price3,
        unit_price4,
        unit_price5,
        unit_price6
    FROM 
        itemprice_mst
    WHERE 
        itemprice_mst.item = item_mst.item
        AND itemprice_mst.site_ref = item_mst.site_ref
        AND effect_date >= '2022-01-01'   --2022-01-01 is first date in Heat_App
    ORDER BY 
        effect_date DESC
) AS latest_price
ORDER BY 
    item_mst.item;
";



            return (await connection.QueryAsync<ItemBookPrice>(sql)).ToList();
        }
        catch (Exception ex)
        {
            // Log the exception (depending on the logging library you're using)
            Console.WriteLine($"Error fetching Book Prices: {ex.Message}");
            // Optionally return an empty list or throw the exception to be handled by the caller
            return new List<ItemBookPrice>();
        }
    }


    public List<CustomerSalesByItem> GetSalesByItemForCustomer(string custNum, DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            using var connection =
                _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

            // Get the RepCode directly from UserService
            var repCode = _userService.CurrentRep.RepCode;

            // Set default date range for last year if dates are not provided
            if (startDate == null || endDate == null)
            {
                endDate ??= DateTime.Today;
                startDate ??= endDate.Value.AddDays(-365);
            }

            var sql = @"
            SELECT ih.cust_num, ii.item, 
                   SUM(ii.qty_invoiced) AS InvQty, 
                   SUM(ii.qty_invoiced * ii.price) AS InvDollars 
            FROM inv_item_mst ii 
            JOIN inv_hdr_mst ih ON ii.inv_num = ih.inv_num AND ih.inv_seq = ii.inv_seq
            WHERE ih.cust_num = @CustNum
              AND ih.inv_date BETWEEN @StartDate AND @EndDate
            GROUP BY ih.cust_num, ii.item";

            return connection.Query<CustomerSalesByItem>(sql,
                new { CustNum = custNum, StartDate = startDate, EndDate = endDate }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching Customer sales: {ex.Message}");
            return new List<CustomerSalesByItem>();
        }
    }

    public async Task UpdateProgcontrolTableAsync(PCFHeaderDTO pcfHeader)
    {

        using (var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName))
        {
            // Retrieve the current record from the database
            var oldRecord = await connection.QueryFirstOrDefaultAsync<PCFHeaderEntity>(
                "SELECT * FROM Progcontrol WHERE PCFNum = @PCFNum",
                new { pcfHeader.PcfNum });

            if (oldRecord == null)
            {
                throw new InvalidOperationException("Record not found for update.");
            }

            string? PromoTerms = pcfHeader.PromoPaymentTerms;
            string? PromoPaymentTermsDescription = pcfHeader.PromoPaymentTermsDescription;
            string? StandardPaymentTerms = pcfHeader.StandardPaymentTermsType;
            string? StandardPaymentTermsDescription = pcfHeader.StandardPaymentTermsDescription;
            string? PromoFreightTerms = pcfHeader.PromoFreightTerms;
            string? PromoFreightMinimums = pcfHeader.PromoFreightMinimums;
            string? FreightTerms = pcfHeader.FreightTerms;
            string? FreightMinimums = pcfHeader.FreightMinimums;



            //      if (pcfHeader.LastEditDate < DateTime.Now.AddYears(-5))
            //      {
            //          pcfHeader.LastEditDate = DateTime.Now;
            //      }
            DateTime currentDateTime = DateTime.Now;
            DateTime? ApprovalDateTime = oldRecord.VPSalesDate;

            // How do we handle approval?
            // 
            int oldApproval = oldRecord.Approved;
            int newApproval = 0;
            string newVP = string.Empty;
            string newSMApprover = string.Empty;



            if (oldApproval == 3 && pcfHeader.PCFStatus != 3)
            {
                newApproval = 0;
                newVP = string.Empty;
                newSMApprover = string.Empty;

            }
            else if (oldApproval != 3 && pcfHeader.PCFStatus == 3)
            {
                newApproval = 3;
                newVP = "CMA";
                newSMApprover = "CMA";
                ApprovalDateTime = currentDateTime;

            }
            else
            {
                newApproval = oldApproval;
                newVP = oldRecord.VPSalesApprovl;
            }

            if (pcfHeader.PcfType != "PW" && pcfHeader.PcfType != "PD") // If it is not 'PW' and not 'PD'
            {
                PromoTerms = string.Empty;
                PromoPaymentTermsDescription = string.Empty;
            }
            else // this is a promo
            {
                FreightTerms = !string.IsNullOrWhiteSpace(PromoFreightTerms) ? PromoFreightTerms : FreightTerms;
                FreightMinimums = !string.IsNullOrWhiteSpace(PromoFreightMinimums)
                    ? PromoFreightMinimums
                    : FreightMinimums;
            }


            var sql = @"
            UPDATE Progcontrol
            SET 
                EditBy = @LastEditedBy,
                EditDate = @LastEditDate,
                EditNotes = @LastEditNotes,
                ProgSDate = @StartDate,
                ProgEDate = @EndDate,
                PCFStatus = @PCFStatus,
                PcfType = @PcfType,
                BTAddr = @BillToAddress,
                CustContact = @CustContact,
                SRNum = @RepID,
                BTCity = @BillToCity,
                Email = @CustContactEmail,
                RepPhone = @RepPhone,
                BTState = @BTState,
                ----Approved = @ApprovalValue,
                ----SalesManager = @SalesManager,
                BTZip = @BTZip,
                Buyer = @Buyer,
                Promo_Terms = @PromoPaymentTerms,
                Promo_Terms_Text = @PromoPaymentTermsDescription,
                Standard_Terms = @StandardPaymentTerms,
                Standard_Terms_Text = @StandardPaymentTermsDescription,
                Standard_Freight_Terms = @FreightTerms,
                Freight_Minimums = @FreightMinimums,
EUT = @EUT,
CustName = @CustomerName,
BTName = @CustomerName,
SalesMngrApproval = @SalesMngrApproval,
SalesMngrDate = @SalesMngrDate,
VPSalesDate = @VPSalesDate,
LastUpdatedBy = @LastUpdatedBy,
LastUpdatedDate = @LastUpdatedDate,
BTPhone = @BillToPhone,
GenNotes = @GeneralNotes,
BuyerEmail = @BuyerEmail,
BuyerPhone = @BuyerPhone,
VPSalesApprovl = @newVP,
Approved = @newApproval


            WHERE PCFNum = @PCFNum";

            var parameters = new
            {
                pcfHeader.LastEditedBy,
                pcfHeader.LastEditDate,
                pcfHeader.LastEditNotes,
                pcfHeader.StartDate,
                pcfHeader.EndDate,
                pcfHeader.PCFStatus,
                pcfHeader.PcfType,
                pcfHeader.BillToAddress,
                pcfHeader.CustContact,
                pcfHeader.RepName,
                pcfHeader.BillToCity,
                pcfHeader.CustContactEmail,
                pcfHeader.RepPhone,
                pcfHeader.BTState,
                //           pcfHeader.ApprovalValue,      //pcfHeader.ApprovalValue
                //           pcfHeader.SalesManager,
                pcfHeader.ApprovalStatus,
                pcfHeader.BTZip,
                pcfHeader.Buyer,
                pcfHeader.RepID,
                PromoPaymentTerms = PromoTerms,
                PromoPaymentTermsDescription = PromoPaymentTermsDescription,
                StandardPaymentTerms,
                StandardPaymentTermsDescription,
                FreightTerms, // promofreightterms if promo, std if not
                FreightMinimums, // same
                pcfHeader.PcfNum,
                pcfHeader.EUT,
                pcfHeader.CustomerName,
                SalesMngrApproval = newSMApprover,
                SalesMngrDate = ApprovalDateTime,
                VPSalesDate = ApprovalDateTime,
                LastUpdatedBy = _userService.UserName,
                LastUpdatedDate = currentDateTime,
                pcfHeader.CustomerInfo.BillToPhone,
                pcfHeader.GeneralNotes,
                pcfHeader.BuyerEmail,
                pcfHeader.BuyerPhone,
                newVP,
                newApproval,
                PCFNumber = pcfHeader.PcfNum.ToString()
            };


            await connection.ExecuteAsync(sql, parameters);
            // Compare old and new values and log changes
            var newRecord = await connection.QueryFirstOrDefaultAsync<PCFHeaderEntity>(
                "SELECT * FROM Progcontrol WHERE PCFNum = @PCFNum",
                new { pcfHeader.PcfNum });

            await LogChangesAsync(oldRecord, newRecord, "Progcontrol", _userService.UserName, currentDateTime);

        }




    }

    public async Task UpsertPCFItemsAsync(List<PCFItemDTO> pcfLines)
    {
        var sqlUpdate = @"
        UPDATE PCItems
        SET 
            ItemDesc = @ItemDesc,
            ProposedPrice = @ProposedPrice,
            LastUpdatedBy = @UserName,
            LastUpdatedDate = GETDATE()

        WHERE 
            PCFNumber = @PCFNumber AND 
            ItemNum = @ItemNum";

        var sqlInsert = @"
        INSERT INTO PCItems (PCFNumber, ItemNum, ItemDesc, ProposedPrice, CustNum, LastUpdatedBy, LastUpdatedDate)
        VALUES (@PCFNumber, @ItemNum, @ItemDesc, @ProposedPrice, @CustNum, @UserName, GETDATE())";

        using (var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName))
        {

            foreach (var item in pcfLines)
            {
                // Add the UserName parameter to each item
                item.UserName = _userService.UserName; // Set this variable to the current username

                var rowsAffected = await connection.ExecuteAsync(sqlUpdate, item);
                if (rowsAffected == 0)
                {
                    await connection.ExecuteAsync(sqlInsert, item);
                }
            }
        }
    }

    public string GetDatabaseNameFromDatabaseKey(string databaseKey)
    {
        // Retrieve the value from the appsettings.json file
        string databaseName = _configuration[$"DBNames:{databaseKey}"];

        if (string.IsNullOrEmpty(databaseName))
        {
            throw new ArgumentException($"Invalid DatabaseKey: {databaseKey}. Please provide a valid key.");
        }

        return databaseName;
    }

    public string GetSytelineDatabaseNameFromDatabaseKey(string databaseKey)
    {
        // Retrieve the value from the appsettings.json file
        if (databaseKey == "PCFDB")
        {
            databaseKey = "Syteline";
        }
        else
        {
            databaseKey = "SytelineHeath";
        }

        string databaseName = _configuration[$"DBNames:{databaseKey}"];

        if (string.IsNullOrEmpty(databaseName))
        {
            throw new ArgumentException($"Invalid DatabaseKey: {databaseKey}. Please provide a valid key.");
        }

        return databaseName;
    }

    private async Task LogChangesAsync<T>(
        T oldRecord,
        T newRecord,
        string tableName,
        string changedBy,
        DateTime changedDate
    ) where T : class
    {
        var oldValues = oldRecord.GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(oldRecord, null));
        var newValues = newRecord.GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(newRecord, null));

        var changes = newValues
            .Where(kvp => oldValues.ContainsKey(kvp.Key) && !Equals(oldValues[kvp.Key], kvp.Value))
            .Select(kvp => new
            {
                FieldName = kvp.Key, OldValue = oldValues[kvp.Key]?.ToString(), NewValue = kvp.Value?.ToString()
            });

        using (var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName))
        {
            foreach (var change in changes)
            {
                var auditLog = new
                {
                    TableName = tableName,
                    RecordKey = oldValues["PCFNum"]?.ToString(), // Assuming PcfNum is the key
                    FieldName = change.FieldName,
                    OldValue = change.OldValue,
                    NewValue = change.NewValue,
                    ChangedBy = changedBy,
                    ChangedDate = changedDate
                };

                await connection.ExecuteAsync(@"
            INSERT INTO PcfAuditLog (TableName, RecordKey, FieldName, OldValue, NewValue, ChangedBy, ChangedDate)
            VALUES (@TableName, @RecordKey, @FieldName, @OldValue, @NewValue, @ChangedBy, @ChangedDate)",
                    auditLog);
            }
        }
    }

    public async Task<int> RefreshConsolidatedCustomers()
    {
        using (var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName))
        {
            var parameters = new DynamicParameters();
            parameters.Add("@RecordsInserted", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var result = await connection.ExecuteAsync("sp_UpdateConsolidatedCustomers",
                parameters,
                commandType: CommandType.StoredProcedure);
            return
                parameters.Get<int>("@RecordsInserted"); // Assuming the stored procedure returns the number of records
        }
    }


    public async Task<List<PcfAuditLog>> GetAuditLogs(string recordKey)
    {
        using (var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName))
        {
            var query = @"
            SELECT Id, TableName, RecordKey, FieldName, OldValue, NewValue, ChangedBy, ChangedDate
            FROM PcfAuditLog
            WHERE RecordKey = @RecordKey
            ORDER BY ChangedDate";

            return (await connection.QueryAsync<PcfAuditLog>(query, new { RecordKey = recordKey })).ToList();
        }



    }

    public async Task<CustomerBuyer> GetCustomerBuyerInfo(string custNum)
    {
        using (var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName))
        {
            var query = @"
            SELECT *
            FROM CustomerBuyerInformation
            WHERE CustNum = @custNum";



            return await connection.QueryFirstOrDefaultAsync<CustomerBuyer>(query, new { custNum = custNum });
        }
    }


    public async Task<string> GetLatestBuyerEmailAsync(string custNum)
    {
        if (string.IsNullOrWhiteSpace(custNum))
        {
            return string.Empty; // Avoid querying with null/empty CustNum
        }

        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);

        var email = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT TOP 1 BuyerEmail FROM CustomerBuyerInformation WHERE LTRIM(RTRIM(CustNum)) = LTRIM(RTRIM(@CustNum))",
            new { CustNum = custNum });

        return email ?? string.Empty; // Ensure non-null return
    }

    public async Task<string> GetSalesManagerEmailAsync(string SalesManager)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SalesManager))
            {
                return string.Empty; // Avoid querying with null/empty SalesManager
            }

            using var connection =
                _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

            var email = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT TOP 1 SalesManagerEmail FROM Chap_SalesManagers WHERE SalesManagerInitials = @SalesManager",
                new { SalesManager = SalesManager });

            return email ?? string.Empty; // Ensure non-null return
        }
        catch (Exception ex)
        {
            // Log the exception to the console
            Console.WriteLine($"Error fetching Sales Manager email: {ex.Message}");
            return string.Empty; // Return an empty string to indicate failure
        }
    }




    public async Task<string> GetSalesRepEmailAsync(string repCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repCode))
            {
                return string.Empty; // Avoid querying with null/empty repCode
            }

            using var connection =
                _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

            var email = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT TOP 1 EmailList FROM Chap_SalesRepEmail WHERE RepCode = @RepCode",
                new { RepCode = repCode });

            return email ?? string.Empty; // Ensure non-null return
        }
        catch (Exception ex)
        {
            // Log the exception to the console
            Console.WriteLine($"Error fetching Sales Rep email: {ex.Message}");
            return string.Empty; // Return an empty string to indicate failure
        }
    }












    public async Task<List<ExpandoObject>> GetExpiringPCFsReportAsync()
    {
        using (var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName))
        {
            //await connection.OpenAsync();

            var sql = @"
DECLARE @cols NVARCHAR(MAX), @query NVARCHAR(MAX);

SELECT @cols = STUFF((SELECT DISTINCT ',' + QUOTENAME(SalesManagerName)
                      FROM custinfo.dbo.Chap_SalesManagers_vw sm
                      WHERE EXISTS (
                          SELECT 1 FROM ciisql10.BAT_App.dbo.customer_mst c
                          INNER JOIN Progcontrol p ON LTRIM(c.cust_num) = p.custnum
                          WHERE c.cust_seq = 0 
                          AND c.uf_c_slsmgr = sm.SalesManagerInitials  
                          AND p.ProgEDate >= GETDATE()
                      )
                      FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '');

IF @cols IS NULL OR @cols = ''
BEGIN
    PRINT 'No Sales Managers have expiring PCFs.';
    RETURN;
END

SET @query = '
WITH ExpiringPCFs AS (
    SELECT 
        c.uf_c_slsmgr AS SalesManagerInitials,
        CASE 
            WHEN DATEDIFF(DAY, GETDATE(), p.ProgEDate) BETWEEN 0 AND 30 THEN ''0-30 Days''
            WHEN DATEDIFF(DAY, GETDATE(), p.ProgEDate) BETWEEN 31 AND 60 THEN ''31-60 Days''
            WHEN DATEDIFF(DAY, GETDATE(), p.ProgEDate) BETWEEN 61 AND 90 THEN ''61-90 Days''
            ELSE ''91+ Days''
        END AS ExpirationRange,
        COUNT(*) AS PCF_Count
    FROM Progcontrol p
    INNER JOIN ciisql10.BAT_App.dbo.customer_mst c 
        ON p.custnum = LTRIM(c.cust_num)  
    WHERE c.cust_seq = 0 
      AND c.uf_c_slsmgr IS NOT NULL 
      AND p.ProgEDate >= GETDATE()
AND p.PCFStatus = 3 
    GROUP BY c.uf_c_slsmgr, 
        CASE 
            WHEN DATEDIFF(DAY, GETDATE(), p.ProgEDate) BETWEEN 0 AND 30 THEN ''0-30 Days''
            WHEN DATEDIFF(DAY, GETDATE(), p.ProgEDate) BETWEEN 31 AND 60 THEN ''31-60 Days''
            WHEN DATEDIFF(DAY, GETDATE(), p.ProgEDate) BETWEEN 61 AND 90 THEN ''61-90 Days''
            ELSE ''91+ Days''
        END
),
SalesManagers AS (
    SELECT SalesManagerInitials, SalesManagerName
    FROM custinfo.dbo.Chap_SalesManagers_vw
    WHERE SalesManagerInitials IN (SELECT DISTINCT SalesManagerInitials FROM ExpiringPCFs)
)
SELECT ExpirationRange, ' + STUFF((SELECT ', ISNULL(' + QUOTENAME(SalesManagerName) + ', 0) AS ' + QUOTENAME(SalesManagerName)
                                   FROM custinfo.dbo.Chap_SalesManagers_vw sm
                                   WHERE EXISTS (
                                       SELECT 1 FROM ciisql10.BAT_App.dbo.customer_mst c
                                       INNER JOIN Progcontrol p ON LTRIM(c.cust_num) = p.custnum
                                       WHERE c.cust_seq = 0 
                                       AND c.uf_c_slsmgr = sm.SalesManagerInitials  
                                       AND p.ProgEDate >= GETDATE()                                      
                                   )
                                   FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') + '
FROM (
    SELECT sm.SalesManagerName, e.ExpirationRange, e.PCF_Count
    FROM ExpiringPCFs e
    INNER JOIN SalesManagers sm ON e.SalesManagerInitials = sm.SalesManagerInitials
) AS SourceTable
PIVOT (
    SUM(PCF_Count)
    FOR SalesManagerName IN (' + @cols + ')
) AS PivotTable
ORDER BY 
    CASE ExpirationRange
        WHEN ''0-30 Days'' THEN 1
        WHEN ''31-60 Days'' THEN 2
        WHEN ''61-90 Days'' THEN 3
        ELSE 4
    END;';

EXEC sp_executesql @query;
";

            var result = await connection.QueryAsync(sql);
            var expandoList = new List<ExpandoObject>();

            foreach (var row in result)
            {
                IDictionary<string, object> expando = new ExpandoObject();
                foreach (var property in (IDictionary<string, object>)row)
                {
                    expando[property.Key] = property.Value ?? 0; // Replace NULLs with 0
                }

                expandoList.Add((ExpandoObject)expando);
            }

            return expandoList;
        }
    }


    public async Task<List<PcfCustomer>> LoadPcfActiveCustomers()
    {




        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);

        var query =
            "SELECT DISTINCT LTRIM(RTRIM(CustNum)) as CustNum, LTRIM(RTRIM(CustName)) as CustName FROM progcontrol WHERE pcfstatus = 3 ORDER BY custNum";

        return (await connection.QueryAsync<PcfCustomer>(query)).ToList();
    }

    public async Task<List<PcfItem>> LoadPcfItems()
    {




        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);

        string query = @"SELECT DISTINCT pcfnumber, ItemNum, ItemDesc 
                         FROM pcitems pi 
                         JOIN progcontrol pr ON pr.PCFNum = CAST(pi.pcfnumber AS INT) 
                         WHERE  pr.pcfstatus = 3 Order by ItemDesc";

        var result = (await connection.QueryAsync<PcfItem>(query)).ToList();
        return result;
    }


    public async Task<List<PcfPCF>> LoadPcfPCFs(string custNum)
    {

        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);

        string query = @"SELECT DISTINCT pcfnum FROM progcontrol WHERE pcfstatus = 3 AND CustNum = @CustNum";

        return (await connection.QueryAsync<PcfPCF>(query, new { CustNum = custNum })).ToList();
    }


    public async Task<List<PCFHeaderDTO>> GetAllPCFHeadersWithItemsAsync()
    {
        string sql = @"
    SELECT 
        h.PCFNum, 
        h.CustNum as CustomerNumber, 
        h.CustName as CustomerName, 
        h.ProgSDate as StartDate, 
        h.ProgEDate as EndDate, 
        h.PCFStatus, 
        h.PcfType, 
        h.VPSalesDate,
        h.BuyingGroup, 
        h.SubmittedBy,
        h.GenNotes as GeneralNotes,
        h.Promo_Terms_Text as PromoPaymentTermsText,
        h.Standard_Freight_Terms as PromoFreightTerms,
        h.Freight_Minimums as FreightMinimums,
        cc.SalesManager,
        cc.AddressLine1 as BillToAddress,
        cc.City as BillToCity,
        cc.State as BTState,
        cc.Zip as BTZip,
        i.PCFNumber,
        i.ItemNum,
        it.Stat as ItemStatus, 
        i.CustNum,
        i.ItemDesc,
        i.ProposedPrice as ApprovedPrice
    FROM ProgControl h 
    LEFT JOIN PCItems i 
        ON CAST(h.PCFNum AS varchar(50)) = i.PCFNumber
    LEFT JOIN ConsolidatedCustomers cc 
        ON h.CustNum = cc.CustNum AND cc.CustSeq = 0
    LEFT JOIN CIISQL10.Bat_App.dbo.Item_mst it 
        ON i.ItemNum = it.Item
    WHERE pcfnum > 0 and (h.ProgSDate > '1/1/2019' or h.PCFStatus = 3) and h.PCFStatus <> 98";

        _logger.LogInformation($"GetAllPCFHeadersWithItemsAsync: {sql}");
        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        var headerDict = new Dictionary<int, PCFHeaderDTO>();

        var result = await connection.QueryAsync<PCFHeaderDTO, PCFItemDTO, PCFHeaderDTO>(
            sql,
            (header, item) =>
            {
                if (!headerDict.TryGetValue(header.PcfNum, out var currentHeader))
                {
                    currentHeader = header;
                    currentHeader.PCFLines = new List<PCFItemDTO>();
                    headerDict.Add(currentHeader.PcfNum, currentHeader);
                }

                if (item != null)
                {
                    currentHeader.PCFLines.Add(item);
                }

                return currentHeader;
            },
            new { RepCode = "not used" },
            splitOn: "PCFNumber"
        );

        var allHeaders = headerDict.Values.ToList();



        return allHeaders;
    }

    public async Task<List<PCFDetail>> GetAllPCFDetailsAsync()
    {
        string sql = @"
    SELECT 
        h.PCFNum, 
        h.CustNum as CustomerNumber, 
        h.CustName as CustomerName, 
        h.ProgSDate as StartDate, 
        h.ProgEDate as EndDate, 
        h.PCFStatus, 
        h.PcfType, 
        h.VPSalesDate,
        h.BuyingGroup, 
        h.SubmittedBy,
        h.GenNotes as GeneralNotes,
        h.Promo_Terms_Text as PromoPaymentTermsText,
        h.Standard_Freight_Terms as PromoFreightTerms,
        h.Freight_Minimums as FreightMinimums,
        cc.SalesManager,
        cc.AddressLine1 as BillToAddress,
        cc.City as BillToCity,
        cc.State as BTState,
        cc.Zip as BTZip,
        cc.EUT,
        i.PCFNumber,
        i.ItemNum,
        it.Stat as ItemStatus, 
        i.CustNum,
        i.ItemDesc,
        i.ProposedPrice as ApprovedPrice
        ,isnull(it.Uf_PrivateLabel,0) as PrivateLabelFlag
        ,it.Family_Code, fc.Description as FamilyCodeDescription
        ,LTRIM(RTRIM(coalesce(h.SRNum, cc.Salesman, ''))) as Salesman
    FROM ProgControl h 
    LEFT JOIN PCItems i 
        ON CAST(h.PCFNum AS varchar(50)) = i.PCFNumber
    LEFT JOIN ConsolidatedCustomers cc 
        ON h.CustNum = cc.CustNum AND cc.CustSeq = 0
    LEFT JOIN CIISQL10.Bat_App.dbo.Item_mst it 
        ON i.ItemNum = it.Item
    LEFT JOIN CIISQL10.Bat_App.dbo.famcode_mst fc on fc.family_code = it.family_code
    WHERE pcfnum > 0 and (h.ProgSDate > @StartDate or h.PCFStatus = 3) and h.PCFStatus <> 98";

        _logger.LogInformation($"GetAllPCFDetailsAsync: {sql}");
        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        var headerDict = new Dictionary<int, PCFHeaderDTO>();

        var result = await connection.QueryAsync<PCFDetail>(sql, new { StartDate = new DateTime(2019, 1, 1) });

        return result.ToList();
    }




    public async Task<List<PCFDetail>> GetAllPCFDetailsWithQtyAsync()
    {
        string sql = @"
  SELECT 
    h.PCFNum, 
    h.CustNum as CustomerNumber, 
    h.CustName as CustomerName, 
    h.ProgSDate as StartDate, 
    h.ProgEDate as EndDate, 
    h.PCFStatus, 
    h.PcfType, 
    h.VPSalesDate,
    h.BuyingGroup, 
    h.SubmittedBy,
    h.GenNotes as GeneralNotes,
    h.Promo_Terms_Text as PromoPaymentTermsText,
    h.Standard_Freight_Terms as PromoFreightTerms,
    h.Freight_Minimums as FreightMinimums,
    cc.SalesManager,
    cc.AddressLine1 as BillToAddress,
    cc.City as BillToCity,
    cc.State as BTState,
    cc.Zip as BTZip,
    cc.EUT,
    i.PCFNumber,
    i.ItemNum,
    it.Stat as ItemStatus, 
    i.CustNum,
    i.ItemDesc,
    i.ProposedPrice as ApprovedPrice,
    ISNULL(it.Uf_PrivateLabel,0) as PrivateLabelFlag,
    it.Family_Code, 
    fc.Description as FamilyCodeDescription,
    LTRIM(RTRIM(COALESCE(h.SRNum, cc.Salesman, ''))) as Salesman,
    LTRIM(RTRIM(COALESCE(ca0.Corp_Cust,'N/A'))) as CorpCustNum,
    ISNULL(p.FY2023_Qty, 0) as FY2023_Qty,
    ISNULL(p.FY2024_Qty, 0) as FY2024_Qty,
    ISNULL(p.FY2025_Qty, 0) as FY2025_Qty,
    ISNULL(p.FY2026_Qty, 0) as FY2026_Qty,
    ISNULL(p.FY2027_Qty, 0) as FY2027_Qty,
    ISNULL(p.FY2028_Qty, 0) as FY2028_Qty,
   ISNULL(p.FY2023_Sales, 0) as FY2023_Qty,
    ISNULL(p.FY2024_Sales, 0) as FY2024_Sales,
    ISNULL(p.FY2025_Sales, 0) as FY2025_Sales,
    ISNULL(p.FY2026_Sales, 0) as FY2026_Sales,
    ISNULL(p.FY2027_Sales, 0) as FY2027_Sales,
    ISNULL(p.FY2028_Sales, 0) as FY2028_Sales,

    -- NEW: 1 if (PCF, Item) exists in PcItemsDeleteLater; 0 otherwise
    CAST(CASE WHEN d.PcfNum IS NOT NULL THEN 1 ELSE 0 END AS bit) AS DeleteLater

FROM ProgControl h 
LEFT JOIN PCItems i 
    ON CAST(h.PCFNum AS varchar(50)) = i.PCFNumber
LEFT JOIN ConsolidatedCustomers cc 
    ON h.CustNum = cc.CustNum AND cc.CustSeq = 0
LEFT JOIN CIISQL10.Bat_App.dbo.Item_mst it 
    ON i.ItemNum = it.Item
LEFT JOIN CIISQL10.Bat_App.dbo.famcode_mst fc 
    ON fc.family_code = it.family_code
LEFT JOIN CIISQL10.Bat_App.dbo.custaddr_mst ca0 on h.custnum = ltrim(rtrim(ca0.Cust_Num)) and ca0.cust_seq = 0
 
-- NEW: join to Delete-Later (use DISTINCT to avoid accidental dup rows)
LEFT JOIN (
    SELECT DISTINCT PcfNum, ItemNum
    FROM PcItemsDeleteLater
) AS d
    ON d.PcfNum = h.PCFNum
   AND d.ItemNum = i.ItemNum
LEFT JOIN OPENQUERY([ciisql10], '
    SELECT
        ih.cust_num,
        ii.item,
        SUM(CASE WHEN fc.FiscalYear = 2023 THEN CAST(ii.qty_invoiced AS decimal(18,4)) ELSE 0 END) AS FY2023_Qty,
        SUM(CASE WHEN fc.FiscalYear = 2024 THEN CAST(ii.qty_invoiced AS decimal(18,4)) ELSE 0 END) AS FY2024_Qty,
        SUM(CASE WHEN fc.FiscalYear = 2025 THEN CAST(ii.qty_invoiced AS decimal(18,4)) ELSE 0 END) AS FY2025_Qty,
        SUM(CASE WHEN fc.FiscalYear = 2026 THEN CAST(ii.qty_invoiced AS decimal(18,4)) ELSE 0 END) AS FY2026_Qty,
        SUM(CASE WHEN fc.FiscalYear = 2027 THEN CAST(ii.qty_invoiced AS decimal(18,4)) ELSE 0 END) AS FY2027_Qty,
        SUM(CASE WHEN fc.FiscalYear = 2028 THEN CAST(ii.qty_invoiced AS decimal(18,4)) ELSE 0 END) AS FY2028_Qty,
        SUM(CASE WHEN fc.FiscalYear = 2023 THEN CAST(ii.qty_invoiced * ii.price AS decimal(18,4)) ELSE 0 END) AS FY2023_Sales,
        SUM(CASE WHEN fc.FiscalYear = 2024 THEN CAST(ii.qty_invoiced * ii.price AS decimal(18,4)) ELSE 0 END) AS FY2024_Sales,
        SUM(CASE WHEN fc.FiscalYear = 2025 THEN CAST(ii.qty_invoiced * ii.price AS decimal(18,4)) ELSE 0 END) AS FY2025_Sales,
        SUM(CASE WHEN fc.FiscalYear = 2026 THEN CAST(ii.qty_invoiced * ii.price AS decimal(18,4)) ELSE 0 END) AS FY2026_Sales,
        SUM(CASE WHEN fc.FiscalYear = 2027 THEN CAST(ii.qty_invoiced * ii.price AS decimal(18,4)) ELSE 0 END) AS FY2027_Sales,
        SUM(CASE WHEN fc.FiscalYear = 2028 THEN CAST(ii.qty_invoiced * ii.price AS decimal(18,4)) ELSE 0 END) AS FY2028_Sales
    FROM Bat_App.dbo.inv_hdr_mst_all AS ih
    JOIN Bat_App.dbo.inv_item_mst_all AS ii
        ON ih.inv_num = ii.inv_num
    JOIN Bat_App.dbo.co_mst AS co
        ON ih.co_num = co.co_num
    JOIN Bat_App.dbo.coitem_mst AS ci
        ON co.co_num     = ci.co_num
       AND ii.co_release = ci.co_release
       AND ii.co_line    = ci.co_line
    JOIN tempwork.dbo.FiscalCalendarVw AS fc
        ON ih.inv_date = fc.[Date]
    WHERE fc.FiscalYear BETWEEN 2023 AND 2028
    GROUP BY ih.cust_num, ii.item
') AS p
    ON ltrim(p.cust_num) = i.custnum
   AND p.item     = i.itemnum


    WHERE h.pcfnum > 0 and (h.ProgSDate > @StartDate or h.PCFStatus = 3) and h.PCFStatus <> 98";

        _logger.LogInformation($"GetAllPCFDetailsAsync: {sql}");
        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        var headerDict = new Dictionary<int, PCFHeaderDTO>();

        var result = await connection.QueryAsync<PCFDetail>(sql, new { StartDate = new DateTime(2019, 1, 1) });

        return result.ToList();
    }




    public async Task<List<FamilyCode>> GetAllFamilyCodesAsync()
    {




        using var connection = _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

        string query = @"SELECT DISTINCT UPPER(family_code) as family_code, description as family_name
                         FROM famcode_mst 
                         WHERE family_code IS NOT NULL AND family_code <> '' 
                         ORDER BY family_code";

        var result = (await connection.QueryAsync<FamilyCode>(query)).ToList();
        return result;
    }

    public async Task<List<FamilyCode>> GetAllFamilyCodesForSaleableItemsAsync()
    {




        using var connection = _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

        string query = @"SELECT DISTINCT UPPER(fc.family_code) as family_code, fc.description as family_name
                         FROM famcode_mst fc join item_mst im on fc.family_code = im.family_code
                         WHERE fc.family_code IS NOT NULL AND fc.family_code <> '' 
                         and im.active_for_customer_portal = 1
                         order by family_code";

        var result = (await connection.QueryAsync<FamilyCode>(query)).ToList();
        return result;
    }

    public async Task<List<ItemPriceDto>> GetAllItemPricesOnPriceLists()
    {
        var sql = @"SELECT 
    im.item, 
    im.description, 
    UPPER(im.family_code) as family_code,
    fc.description as Family_Code_Description,
    ip.effect_date AS EffectiveDate, 
    ISNULL(ip.unit_price1, 0.0) AS ListPrice,
    ISNULL(ip.unit_price2, 0.0) AS PP1Price,  -- 4K Book
    ISNULL(ip.unit_price3, 0.0) AS PP2Price,  -- 12.5k Book
    ISNULL(ip.unit_price4, 0.0) AS BM1Price,  -- BG Midstates 4k
    ISNULL(ip.unit_price5, 0.0) AS BM2Price,  -- BG Midstates 12.5k
    ISNULL(ip.unit_price6, 0.0) AS FobPrice
    ,im.stat as ItemStatus
FROM 
    itemprice_mst ip
  inner JOIN  item_mst im on im.item = ip.item
            AND im.site_ref = ip.site_ref
LEFT JOIN 
    famcode_mst fc
    ON im.family_code = fc.family_code
            INNER JOIN (
                SELECT item, MAX(effect_date) AS max_date
                  FROM dbo.itemprice_mst
                 GROUP BY item
            ) md
              ON ip.item = md.item
             AND ip.effect_date = md.max_date
WHERE im.active_for_customer_portal = 1
            ORDER BY ip.item;";

        using var connection = _dbConnectionFactory.CreateReadOnlyConnection(_userService.CurrentSytelineDatabaseName);

        IEnumerable<ItemPriceDto> prices = connection.Query<ItemPriceDto>(sql);
        return prices.ToList();

    }


    public async Task RemoveItemsFromPCFAsync(IEnumerable<object> keys)
    {
        if (keys is null)
            throw new ArgumentNullException(nameof(keys));

        // Normalize incoming anonymous objects -> (PCFNumber, ItemNum)
        var keyPairs = new List<(int PCFNumber, string ItemNum)>();
        foreach (var k in keys)
        {
            if (k is null)
                continue;

            var t = k.GetType();
            var pcfProp = t.GetProperty("PCFNum") ?? t.GetProperty("PCFNumber");
            var itemProp = t.GetProperty("ItemNum");
            if (pcfProp == null || itemProp == null)
                throw new ArgumentException("Each key must have PCFNum (or PCFNumber) and ItemNum properties.");

            var pcfVal = pcfProp.GetValue(k);
            var itemVal = itemProp.GetValue(k);

            if (pcfVal is null || itemVal is null)
                continue;

            keyPairs.Add(((int)Convert.ChangeType(pcfVal, typeof(int)),
                (string)Convert.ChangeType(itemVal, typeof(string))));
        }

        if (keyPairs.Count == 0)
            return;

        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        if (connection.State != ConnectionState.Open)
            connection.Open();

        using var tx = connection.BeginTransaction();

        try
        {
            // Group by PCF to keep the ProgControl edit notes accurate per PCF
            var groups = keyPairs.GroupBy(k => k.PCFNumber);

            foreach (var grp in groups)
            {
                var pcfNumber = grp.Key;
                var items = grp.Select(x => x.ItemNum).Distinct().ToList();
                if (items.Count == 0)
                    continue;

                // Build a VALUES list for a table variable using fully-parameterized values
                var valuesSb = new StringBuilder();
                var dp = new DynamicParameters();

                int i = 0;
                foreach (var pair in grp)
                {
                    var pPcf = $"@pcf{i}";
                    var pItem = $"@item{i}";
                    if (valuesSb.Length > 0)
                        valuesSb.Append(",");
                    valuesSb.Append($"({pPcf}, {pItem})");

                    dp.Add(pPcf, pair.PCFNumber, DbType.Int32);
                    dp.Add(pItem, pair.ItemNum, DbType.String);
                    i++;
                }

                // Optional metadata for archive (if you have these columns; otherwise remove them)
                // Example: if PCItems_DeletedArchive has extra columns like DeletedOnUtc, DeletedReason, DeletedBy
                // you can add them in the SELECT with literals. Here we assume schemas match 1:1.
                Console.WriteLine(valuesSb);
                var sql = $@"
DECLARE @Keys TABLE (PCFNumber INT, ItemNum NVARCHAR(100));
INSERT INTO @Keys (PCFNumber, ItemNum) VALUES {valuesSb};


-- 1) Archive first
INSERT INTO PCItems_DeletedArchiveTesting
SELECT pi.*
FROM PCItems pi
JOIN @Keys k
  ON k.PCFNumber = pi.PCFNumber
 AND k.ItemNum    = pi.ItemNum;

-- 2) Delete from live table
DELETE pi
FROM PCItemsTesting pi
JOIN @Keys k
  ON k.PCFNumber = pi.PCFNumber
 AND k.ItemNum    = pi.ItemNum;";

                await connection.ExecuteAsync(sql, dp, tx, commandTimeout: 60);

                // 3) Update ProgControl.EditNotes for this PCF
                var itemList = string.Join(", ", items);
                var stamp = DateTime.Now.ToString("yyyy-MM-dd"); // or DateTime.UtcNow if you prefer
                var appendText = $"  {stamp} Deleted items {itemList} from PCF";

                // PCFNum in ProgControl is a string key
                var pcfStringKey = pcfNumber.ToString();

                var updateNotesSql = @"
UPDATE ProgControlTesting
SET EditNotes = COALESCE(EditNotes, '') + @AppendText
WHERE PCFNum = @PCFNum;";

                await connection.ExecuteAsync(updateNotesSql,
                    new { AppendText = appendText, PCFNum = pcfStringKey }, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }


    public async Task MarkItemsForDeletionAsync(IEnumerable<object> laterRequests)
    {
        if (laterRequests is null)
            throw new ArgumentNullException(nameof(laterRequests));

        // Normalize: accept objects with { PCFNum or PCFNumber, ItemNum, Reason? }
        var rows = new List<(int PcfNum, string ItemNum, string Reason)>();
        foreach (var r in laterRequests)
        {
            if (r is null)
                continue;

            var t = r.GetType();
            var pcfProp = t.GetProperty("PCFNum") ?? t.GetProperty("PCFNumber") ?? t.GetProperty("PcfNum");
            var itemProp = t.GetProperty("ItemNum");
            var reasonProp = t.GetProperty("Reason");

            if (pcfProp == null || itemProp == null)
                throw new ArgumentException("Each request must include PCFNum (or PCFNumber) and ItemNum.");

            var pcfVal = pcfProp.GetValue(r);
            var itemVal = itemProp.GetValue(r);
            var reasonVal = reasonProp?.GetValue(r);

            if (pcfVal is null || itemVal is null)
                continue;

            var reason = Convert.ToString(reasonVal);
            if (string.IsNullOrWhiteSpace(reason))
                reason = "Slow Sales";

            rows.Add(((int)Convert.ChangeType(pcfVal, typeof(int)),
                      (string)Convert.ChangeType(itemVal, typeof(string)),
                      reason));
        }

        if (rows.Count == 0)
            return;

        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        if (connection.State != ConnectionState.Open)
            connection.Open();
        using var tx = connection.BeginTransaction();

        try
        {
            // Build parameterized VALUES list
            var valuesSb = new StringBuilder();
            var dp = new DynamicParameters();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var pPcf = $"@pcf{i}";
                var pItem = $"@item{i}";
                var pReason = $"@reason{i}";

                if (valuesSb.Length > 0)
                    valuesSb.Append(",");
                valuesSb.Append($"({pPcf}, {pItem}, {pReason})");

                dp.Add(pPcf, r.PcfNum, DbType.Int32);
                dp.Add(pItem, r.ItemNum, DbType.String);
                dp.Add(pReason, r.Reason, DbType.String);
            }

            var sql = $@"
DECLARE @Keys TABLE (PcfNum INT NOT NULL, ItemNum NVARCHAR(100) NOT NULL, Reason NVARCHAR(200) NULL);
INSERT INTO @Keys (PcfNum, ItemNum, Reason) VALUES {valuesSb};

-- Upsert: insert new; if exists, update Reason and DateAdded
MERGE INTO PcItemsDeleteLater AS tgt
USING (
    SELECT PcfNum, ItemNum, COALESCE(NULLIF(Reason, ''), 'Slow Sales') AS Reason
    FROM @Keys
) AS src
ON (tgt.PcfNum = src.PcfNum AND tgt.ItemNum = src.ItemNum)
WHEN MATCHED THEN UPDATE
    SET tgt.Reason = src.Reason,
        tgt.DateAdded = SYSDATETIME()
WHEN NOT MATCHED THEN INSERT (PcfNum, ItemNum, DateAdded, Reason)
    VALUES (src.PcfNum, src.ItemNum, SYSDATETIME(), src.Reason);";

            await connection.ExecuteAsync(sql, dp, tx, commandTimeout: 60);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }



    public async Task UnmarkItemsForDeletionAsync(IEnumerable<object> keys)
    {
        if (keys is null)
            throw new ArgumentNullException(nameof(keys));

        var rows = new List<(int PcfNum, string ItemNum)>();
        foreach (var k in keys)
        {
            if (k is null)
                continue;

            var t = k.GetType();
            var pcfProp = t.GetProperty("PCFNum") ?? t.GetProperty("PCFNumber") ?? t.GetProperty("PcfNum");
            var itemProp = t.GetProperty("ItemNum");

            if (pcfProp == null || itemProp == null)
                throw new ArgumentException("Each key must include PCFNum (or PCFNumber) and ItemNum.");

            var pcfVal = pcfProp.GetValue(k);
            var itemVal = itemProp.GetValue(k);
            if (pcfVal is null || itemVal is null)
                continue;

            rows.Add(((int)Convert.ChangeType(pcfVal, typeof(int)),
                      (string)Convert.ChangeType(itemVal, typeof(string))));
        }

        if (rows.Count == 0)
            return;

        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        if (connection.State != ConnectionState.Open)
            connection.Open();
        using var tx = connection.BeginTransaction();

        try
        {
            var valuesSb = new StringBuilder();
            var dp = new DynamicParameters();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var pPcf = $"@pcf{i}";
                var pItem = $"@item{i}";
                if (valuesSb.Length > 0)
                    valuesSb.Append(",");
                valuesSb.Append($"({pPcf}, {pItem})");
                dp.Add(pPcf, r.PcfNum, DbType.Int32);
                dp.Add(pItem, r.ItemNum, DbType.String);
            }

            var sql = $@"
DECLARE @Keys TABLE (PcfNum INT NOT NULL, ItemNum NVARCHAR(100) NOT NULL);
INSERT INTO @Keys (PcfNum, ItemNum) VALUES {valuesSb};

DELETE tgt
FROM PcItemsDeleteLater AS tgt
JOIN @Keys k
  ON k.PcfNum = tgt.PcfNum
 AND k.ItemNum = tgt.ItemNum;";

            await connection.ExecuteAsync(sql, dp, tx, commandTimeout: 60);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }










    // Persist the "delete later" request for separate handling
    public async Task MarkItemsForDeletionAsyncWithEditNotes(IEnumerable<object> laterRequests)  //Not used

    {

        if (laterRequests is null) throw new ArgumentNullException(nameof(laterRequests));

        // Normalize: accept objects with PCFNum/PCFNumber, ItemNum, Reason (optional)
        var items = new List<(int PcfNum, string ItemNum, string Reason)>();
        foreach (var r in laterRequests)
        {
            if (r is null) continue;

            var t = r.GetType();
            var pcfProp = t.GetProperty("PCFNum") ?? t.GetProperty("PCFNumber") ?? t.GetProperty("PcfNum");
            var itemProp = t.GetProperty("ItemNum");
            var reasonProp = t.GetProperty("Reason");

            if (pcfProp == null || itemProp == null)
                throw new ArgumentException("Each request must include PCFNum (or PCFNumber) and ItemNum.");

            var pcfVal = pcfProp.GetValue(r);
            var itemVal = itemProp.GetValue(r);
            var reasonVal = reasonProp?.GetValue(r);

            if (pcfVal is null || itemVal is null) continue;

            var reason = Convert.ToString(reasonVal) ?? "Slow Sales";
            if (string.IsNullOrWhiteSpace(reason)) reason = "Slow Sales";

            items.Add(((int)Convert.ChangeType(pcfVal, typeof(int)),
                (string)Convert.ChangeType(itemVal, typeof(string)),
                reason));
        }

        if (items.Count == 0) return;

        using var connection = _dbConnectionFactory.CreateReadWriteConnection(_userService.CurrentPCFDatabaseName);
        if (connection.State != ConnectionState.Open) connection.Open();
        using var tx = connection.BeginTransaction();

        try
        {
            // Group by PCF to append one EditNotes line per PCF
            var groups = items.GroupBy(x => x.PcfNum);

            foreach (var grp in groups)
            {
                // Build a parameterized VALUES list for the @Keys table variable
                var valuesSb = new StringBuilder();
                var dp = new DynamicParameters();
                int i = 0;

                foreach (var row in grp)
                {
                    var pPcf = $"@pcf{i}";
                    var pItem = $"@item{i}";
                    var pReason = $"@reason{i}";

                    if (valuesSb.Length > 0) valuesSb.Append(",");
                    valuesSb.Append($"({pPcf}, {pItem}, {pReason})");

                    dp.Add(pPcf, row.PcfNum, DbType.Int32);
                    dp.Add(pItem, row.ItemNum, DbType.String);
                    dp.Add(pReason, row.Reason, DbType.String);
                    i++;
                }

                // Upsert into PcItemsDeleteLater, and then append EditNotes
                var sql = $@"
DECLARE @Keys TABLE (PcfNum INT NOT NULL, ItemNum NVARCHAR(100) NOT NULL, Reason NVARCHAR(200) NULL);
INSERT INTO @Keys (PcfNum, ItemNum, Reason) VALUES {valuesSb};

-- Upsert: insert new, update existing reason/date
MERGE INTO PcItemsDeleteLater AS tgt
USING (
    SELECT k.PcfNum, k.ItemNum, COALESCE(NULLIF(k.Reason, ''), 'Slow Sales') AS Reason
    FROM @Keys k
) AS src
ON (tgt.PcfNum = src.PcfNum AND tgt.ItemNum = src.ItemNum)
WHEN MATCHED THEN
    UPDATE SET
        tgt.Reason    = src.Reason,
        tgt.DateAdded = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (PcfNum, ItemNum, DateAdded, Reason)
    VALUES (src.PcfNum, src.ItemNum, SYSDATETIME(), src.Reason);

-- Return distinct list for note building (optional but handy if you want to SELECT back)
";

                await connection.ExecuteAsync(sql, dp, tx, commandTimeout: 60);

// Append to ProgControl.EditNotes for this PCF
                var itemList = string.Join(", ", grp.Select(g => g.ItemNum).Distinct());
                var reasonForNote = grp.Select(g => g.Reason).Distinct().Count() == 1
                    ? grp.First().Reason
                    : "Multiple";

                var stamp = DateTime.Now.ToString("yyyy-MM-dd");
                var appendText = $"  {stamp} Queued items {itemList} for delete (reason: {reasonForNote})";

                var updateNotesSql = @"
UPDATE ProgControlTesting
SET EditNotes = COALESCE(EditNotes, '') + @AppendText
WHERE PCFNum = @PCFNum;"; // PCFNum is string in ProgControl

                await connection.ExecuteAsync(updateNotesSql,
                    new { AppendText = appendText, PCFNum = grp.Key.ToString() }, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }



}


public class ExpiringPCFReport
{
    public string ExpirationRange { get; set; }
    // Dynamic columns for each Sales Manager will be populated automatically
}


public class PcfCustomer
{
    public string CustNum { get; set; }
    public string CustName { get; set; }
    // public string DisplayText => $"{CustNum} - {CustName}";
}

public class PcfItem
{
    public string ItemNum { get; set; }
    public string ItemDesc { get; set; }
    //public string DisplayText => $"{ItemNum} - {ItemDesc}";
}
public class PcfPCF
{
    public string PCFNum { get; set; }
}