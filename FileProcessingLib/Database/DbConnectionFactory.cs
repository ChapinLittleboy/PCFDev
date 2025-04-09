using System.Data;
using Microsoft.Data.SqlClient;

namespace FileProcessingLib.Database;

public class DbConnectionFactory
{

    private readonly string _pcfConnectionString;
    private readonly string _erpConnectionString;

    public DbConnectionFactory(string pcfConnectionString, string erpConnectionString)
    {


        _pcfConnectionString = pcfConnectionString;
        _erpConnectionString = erpConnectionString;



    }

    // Method to create a read-write connection
    public IDbConnection CreatePcfDbConnection()
    {
        return new SqlConnection(_pcfConnectionString);
    }

    // Method to create a read-only connection
    public IDbConnection CreateErpConnection()
    {
        return new SqlConnection(_erpConnectionString);

    }
}