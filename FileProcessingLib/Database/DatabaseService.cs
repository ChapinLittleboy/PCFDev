namespace FileProcessingLib.Database;
using Dapper;


public class DatabaseService
{
    private readonly DbConnectionFactory _dbConnectionFactory;



    public DatabaseService(string erpConnectionString, string pcfConnectionString)
    {
        _dbConnectionFactory = new DbConnectionFactory(pcfConnectionString, erpConnectionString);

    }

    // Add methods here as needed



    public Customer GetCustomerInformationForCustomer(string selectedCustomer)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreateErpConnection();



            var sql = @"
            SELECT ca.site_ref, ca.cust_num as CustNum, ca.name as CustName, ca.name as BillToName
            , ca.city as BillToCity, ca.state as BillToState, ca.zip as BillToZip
            , ca.[addr##1] as BillToAddress1, ca.[addr##2] as BillToAddress2
            , cu.terms_code as PaymentTerms, cu.slsman as RepCode, cu.Stat as Status
, cu.Uf_PROG_BASIS as PricingMethod, cu.cust_type as BuyingGroup, cu.end_user_type as EUT
            FROM custaddr_mst ca 
            JOIN customer_mst cu ON ca.cust_num = cu.cust_num 
            AND ca.cust_seq = cu.cust_seq AND ca.site_ref = cu.site_ref AND ca.cust_seq = 0
            AND ca.cust_num = @CustNum
            ORDER BY ca.name";

            return connection.QuerySingleOrDefault<Customer>(sql, new { CustNum = selectedCustomer });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching customer: {ex.Message}");
            return null;
        }
    }

    public int GetNextPCFNum()
    {
        try
        {
            using var connection = _dbConnectionFactory.CreatePcfDbConnection();



            var sql = @"
            Select max(PCFNum) + 1 as NextPCFNum from Progcontrol";

            return connection.QuerySingleOrDefault<int>(sql);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error next PCFnum: {ex.Message}");
            return -1;
        }
    }

    public int CreatePCFHeader(PCFHeaderEntity pcfHeaderEntity, Customer customer, Rep rep)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreatePcfDbConnection();

            var sql = @"
        INSERT INTO ProgControl (
            PCFNum, [Date], Warehouse, Dropship, OtherDropship, OtherWarehouse, DWOther, DWOtherText, ProgSDate,
            ProgEDate, CustNum, CustName, BTName, BTAddr, BTCity, BTState, BTZip, BTPhone, Buyer, RepName, 
            RepEmail, RepAgency,  OtherTerms, BuyingGroup, SubmittedBy, SubmitDate,  
             SRNum, EUT, Standard_Terms, Standard_Terms_Text, 
            Promo_Terms, Promo_Terms_Text, Standard_Freight_Terms, Freight_Minimums, Other_Freight_Minimums
        ) VALUES (
            @PCFNum, @Date, @Warehouse, @Dropship, @OtherDropship, @OtherWarehouse, @DWOther, @DWOtherText, @ProgSDate, 
            @ProgEDate, @CustNum, @CustName, @BTName, @BTAddr, @BTCity, @BTState, @BTZip, @BTPhone, @Buyer, @RepName, 
            @RepEmail, @RepAgency,  @OtherTerms, @BuyingGroup, @SubmittedBy, @SubmitDate,  @SRNum, @EUT, @StandardTerms, @StandardTermsText, 
            @PromoTerms, @PromoTermsText, @StandardFreightTerms, @FreightMinimums, @OtherFreightMinimums
        );
        ";

            pcfHeaderEntity.SubmitDate = DateTime.Now;
            pcfHeaderEntity.Date = DateTime.Now;
            pcfHeaderEntity.PCFNum = GetNextPCFNum();

            var parameters = new
            {
                PCFNum = pcfHeaderEntity.PCFNum,
                Date = pcfHeaderEntity.Date,
                Warehouse = pcfHeaderEntity.Warehouse,
                Dropship = pcfHeaderEntity.Dropship,
                OtherDropship = pcfHeaderEntity.OtherDropship,
                OtherWarehouse = pcfHeaderEntity.OtherWarehouse,
                DWOther = pcfHeaderEntity.DWOther,
                DWOtherText = pcfHeaderEntity.DWOtherText,
                ProgSDate = pcfHeaderEntity.ProgSDate,
                ProgEDate = pcfHeaderEntity.ProgEDate,
                CustNum = customer.CustNum,
                CustName = customer.CustName,
                BTName = customer.BillToName,
                BTAddr = customer.BillToAddress1,
                BTCity = customer.BillToCity,
                BTState = customer.BillToState,
                BTZip = customer.BillToZip,
                BTPhone = customer.BillToPhone,
                Buyer = customer.BillToBuyer,
                RepName = rep.Name,
                RepEmail = rep.Email,
                RepAgency = rep.Agency,
                OtherTerms = pcfHeaderEntity.OtherTerms,
                BuyingGroup = customer.BuyingGroup,
                SubmittedBy = pcfHeaderEntity.SubmittedBy,
                SubmitDate = pcfHeaderEntity.SubmitDate,
                Approved = pcfHeaderEntity.Approved,
                SRNum = pcfHeaderEntity.SRNum,
                EUT = customer.EUT,
                StandardTerms = customer.PaymentTerms,
                PromoTerms = pcfHeaderEntity.Promo_Terms,
                PromoTermsText = pcfHeaderEntity.Promo_Terms_Text,
                StandardFreightTerms = pcfHeaderEntity.Standard_Freight_Terms,
                FreightMinimums = pcfHeaderEntity.Freight_Minimums,
                OtherFreightMinimums = pcfHeaderEntity.Other_Freight_Minimums,
                StandardTermsText = pcfHeaderEntity.Standard_Terms_Text
            };

            Console.WriteLine(sql);
            var rowsAffected = connection.Execute(sql, parameters);
            Console.WriteLine(rowsAffected);
            // Return the PCFNum if the insert was successful, otherwise return 0
            return rowsAffected > 0 ? pcfHeaderEntity.PCFNum : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting PCFHeader: {ex.Message}");
            return -1;
        }
    }

    public int CreatePCFLines(List<PCFItemEntity> pcfItems)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreatePcfDbConnection();

            var sql = @"
            INSERT INTO PCItems (
                PCFNumber, ItemNum, CustNum, ItemDesc, ProposedPrice
            ) VALUES (
                @PCFNumber, @ItemNum, @CustNum, @ItemDesc, @ProposedPrice
            )";

            // Execute the insert for the entire list
            int rowsAffected = connection.Execute(sql, pcfItems);

            return rowsAffected; // Return the number of rows inserted
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting PCF lines: {ex.Message}");
            return -1; // Return -1 to indicate failure
        }
    }

    public Rep GetRepById(int repId)
    {
        const string query = @"
            SELECT 
                RepID AS RepId,
                Contact AS Name,
                Name AS Agency,
                Email,
                Usr,
                Pwd,
                Rep as RepCode  
            FROM 
                RepID
            WHERE 
                RepID = @RepId";


        using var connection = _dbConnectionFactory.CreatePcfDbConnection();
        {
            return connection.QuerySingleOrDefault<Rep>(query, new { RepId = repId });
        }
    }
    public Rep GetRepByRepcode(string repCode, string repName)
    {
        const string query = @"
            SELECT 
                RepID AS RepId,
                Contact AS Name,
                Name AS Agency,
                Email,
                Usr,
                Pwd,
                Rep as RepCode  
            FROM 
                RepID
            WHERE 
                Rep = @RepCode and Contact = @RepName"
            ;


        using var connection = _dbConnectionFactory.CreatePcfDbConnection();
        {
            return connection.QuerySingleOrDefault<Rep>(query, new { RepCode = repCode, RepName = repName });
        }
    }






}