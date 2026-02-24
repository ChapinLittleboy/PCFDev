using Microsoft.Data.SqlClient;
using PcfManager.Models;
using System.Data;

namespace PcfManager.Services;

public class SalesRepEmailService
{
    private readonly IConfiguration _config;

    public SalesRepEmailService(IConfiguration config)
    {
        _config = config;
    }

    private SqlConnection GetConnection()
        => new SqlConnection(_config.GetConnectionString("CiiSQL10rw"));

    public async Task<List<SalesRepEmail>> GetAllAsync()
    {
        var list = new List<SalesRepEmail>();

        using var conn = GetConnection();
        using var cmd = new SqlCommand(@"
            SELECT RepCode, SalesRegion, AgencyName, EmailList
            FROM dbo.Chap_SalesRepEmail
            ORDER BY RepCode, SalesRegion", conn);

        await conn.OpenAsync();
        using var rdr = await cmd.ExecuteReaderAsync();

        while (await rdr.ReadAsync())
        {
            list.Add(new SalesRepEmail
            {
                RepCode = rdr.GetString(0),
                SalesRegion = rdr.GetString(1),
                AgencyName = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                EmailList = rdr.IsDBNull(3) ? null : rdr.GetString(3)
            });
        }

        return list;
    }

    public async Task InsertAsync(SalesRepEmail item)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(@"
            INSERT INTO dbo.Chap_SalesRepEmail
            (RepCode, SalesRegion, AgencyName, EmailList)
            VALUES (@RepCode, @SalesRegion, @AgencyName, @EmailList)", conn);

        cmd.Parameters.AddWithValue("@RepCode", item.RepCode);
        cmd.Parameters.AddWithValue("@SalesRegion", item.SalesRegion);
        cmd.Parameters.AddWithValue("@AgencyName", (object?)item.AgencyName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EmailList", (object?)item.EmailList ?? DBNull.Value);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(SalesRepEmail item)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(@"
            UPDATE dbo.Chap_SalesRepEmail
            SET AgencyName = @AgencyName,
                EmailList = @EmailList
            WHERE RepCode = @RepCode
              AND SalesRegion = @SalesRegion", conn);

        cmd.Parameters.AddWithValue("@RepCode", item.RepCode);
        cmd.Parameters.AddWithValue("@SalesRegion", item.SalesRegion);
        cmd.Parameters.AddWithValue("@AgencyName", (object?)item.AgencyName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EmailList", (object?)item.EmailList ?? DBNull.Value);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string repCode, string salesRegion)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(@"
            DELETE FROM dbo.Chap_SalesRepEmail
            WHERE RepCode = @RepCode
              AND SalesRegion = @SalesRegion", conn);

        cmd.Parameters.AddWithValue("@RepCode", repCode);
        cmd.Parameters.AddWithValue("@SalesRegion", salesRegion);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }
}