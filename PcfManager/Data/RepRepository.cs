using PcfManager.Models;
using Dapper;


namespace PcfManager.Data;

public class RepRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly IConfiguration _configuration;



    public RepRepository(DbConnectionFactory dbConnectionFactory, IConfiguration configuration)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _configuration = configuration;

    }

    public string GetDatabaseName()
    {
        return _configuration.GetValue<string>("DatabaseSettings:PCFDB");
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


        using (var dbConnection = _dbConnectionFactory.CreateReadWriteConnection(GetDatabaseName()))
        {
            return dbConnection.QuerySingleOrDefault<Rep>(query, new { RepId = repId });
        }
    }
    public Rep GetRepByRepcode(string repCode)
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
                Rep = @RepCode";


        using (var dbConnection = _dbConnectionFactory.CreateReadWriteConnection(GetDatabaseName()))
        {
            return dbConnection.QuerySingleOrDefault<Rep>(query, new { Rep = repCode });
        }
    }

    public Rep GetRepByUsername(string userName)
    {
        const string query = @"
            SELECT top 1
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
                Rep = @Rep";


        using (var dbConnection = _dbConnectionFactory.CreateReadWriteConnection(GetDatabaseName()))
        {
            return dbConnection.QuerySingleOrDefault<Rep>(query, new { Rep = "CHAP" });
        }
    }
}