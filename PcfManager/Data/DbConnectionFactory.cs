using Microsoft.Data.SqlClient;
using System.Data;
namespace PcfManager.Data;

public class DbConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly string _readWriteConnectionString;
    private readonly string _readOnlyConnectionString;




    public DbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;


        _readWriteConnectionString = _configuration.GetConnectionString("CiiSQL01"); // Read-write connection
        _readOnlyConnectionString = _configuration.GetConnectionString("CiiSQL10ro"); // Read-only connection

    }

    // Method to create a read-write connection
    public IDbConnection CreateReadWriteConnection(string databaseName = null)
    {
        var connection = new SqlConnection(_readWriteConnectionString);


        if (!string.IsNullOrEmpty(databaseName))
        {
            connection.Open(); // Required to change database
            connection.ChangeDatabase(databaseName);

        }


        return connection;
    }

    // Method to create a read-only connection
    public IDbConnection CreateReadOnlyConnection(string databaseName = null)
    {


        var connection = new SqlConnection(_readOnlyConnectionString);
        if (!string.IsNullOrEmpty(databaseName))
        {
            // Get the database name based on the prefix  for SYTELINE
            databaseName = GetSytelineDatabaseName(databaseName);

            connection.Open(); // Required to change database
            connection.ChangeDatabase(databaseName);


        }
        return connection;
    }

    private string GetSytelineDatabaseName(string databaseName)
    {
        switch (databaseName.Substring(0, 3).ToUpper())
        {
            case "BAT":
                return "BAT_App";
            case "HEA":
                return "HEAT_App";
            default:
                throw new ArgumentException("Invalid database name prefix");
        }
    }
}

