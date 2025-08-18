namespace PcfManager.Models;

public class FamilyCode
{
    public string family_code { get; set; }
    public string family_name { get; set; }

  
    public string? family_display => family_code +  " - " + family_name;



}
