namespace PcfManager.Data;

using Audit.SqlServer.Providers;

public class DynamicSqlServerProvider : SqlDataProvider
{
    private readonly Func<string> _getConnectionString;

    public DynamicSqlServerProvider(Func<string> getConnectionString)
    {
        _getConnectionString = getConnectionString;
    }

    // public override string ConnectionString => _getConnectionString();

    // public override string TableName => "PcfAuditLog"; // You can make this dynamic too if needed
}