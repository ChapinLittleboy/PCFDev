namespace PcfManager.Models;

public class Rep
{


    public int RepId { get; set; }
    public string Usr { get; set; }
    public string Pwd { get; set; }
    public string RepCode { get; set; }

    public string Name { get; set; }
    public string Agency { get; set; }
    public string Email { get; set; }
    public bool LoginValidated { get; set; } = false;


    public Rep() { } // Parameterless constructor for Dapper



}
