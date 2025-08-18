namespace PcfManager.Models;

// Models/PriceResult.cs

    public class PriceResult
    {
        public string FamilyCode { get; set; } = "";
        public string ItemCode { get; set; } = "";
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
    }



    public enum PriceSourceKind
    {
        PCF,
        StaticList
    }
