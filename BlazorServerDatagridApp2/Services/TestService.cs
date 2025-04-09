namespace BlazorServerDatagridApp2.Services;

using Microsoft.Extensions.Configuration;

public class TestService
{
    private readonly IConfiguration _configuration;

    public TestService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void TestReadSettings()
    {
        var pcfdDb = _configuration["DBNames:PCFDB"];
        var pcfdDbHeath = _configuration["DBNames:PCFDBHeath"];
        var syteline = _configuration["DBNames:Syteline"];

        Console.WriteLine($"PCFDB: {pcfdDb}");
        Console.WriteLine($"PCFDBHeath: {pcfdDbHeath}");
        Console.WriteLine($"Syteline: {syteline}");
    }
}
