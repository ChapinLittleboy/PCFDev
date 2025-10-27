namespace PcfManager.Models;

public class PCFDetail
{

        public int PCFNum { get; set; }
        public string? CustomerNumber { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PCFStatus { get; set; }
        public string? PcfType { get; set; }
        public DateTime? VPSalesDate { get; set; }
        public string? BuyingGroup { get; set; }
        public string? GeneralNotes { get; set; }
        public string? PromoPaymentTermsText { get; set; }
        public string? PromoFreightTerms { get; set; }
        public string? FreightMinimums { get; set; }  // this will hold Promo Freight Minimums
        public string? SalesManager { get; set; }
        public string? BillToAddress { get; set; }
        public string? BillToCity { get; set; }
        public string? BTState { get; set; }
        public string? BTZip { get; set; }
        public string PCFNumber { get; set; }
        public string? ItemNum { get; set; }
        public string? ItemStatus { get; set; }
        public string? CustNum { get; set; }
        public string? ItemDesc { get; set; }
        public string? ApprovedPrice { get; set; }
        public int? PrivateLabelFlag { get; set; } = 0;
        public string? Family_Code { get; set; }
        public string? Family_Code_Description { get; set; }
        public string? Salesman { get; set; }
        public string? EUT { get; set; }
        public bool DeleteNow { get; set; }
        public bool DeleteLater { get; set; } // not in DB, used in UI to mark for deletion
        public string? CorpCustNum { get; set; }

    public string? StandardFreightTerms { get; set; }

    public string? StandardFreightMinimums { get; set; }

    public decimal FY2023_Qty { get; set; }
        public decimal FY2024_Qty { get; set; }
        public decimal FY2025_Qty { get; set; }
        public decimal FY2026_Qty { get; set; }
        public decimal FY2027_Qty { get; set; }
        public decimal FY2028_Qty { get; set; }


    public decimal FY2023_Sales { get; set; }
    public decimal FY2024_Sales { get; set; }
    public decimal FY2025_Sales { get; set; }
    public decimal FY2026_Sales { get; set; }
    public decimal FY2027_Sales { get; set; }
    public decimal FY2028_Sales { get; set; }

    public decimal? StandardCost { get; set; }

    public decimal? StandardMargin
    {
        get
        {
            // Return null if we can’t calculate
            if ( ApprovedPriceDecimal == 0)
                return null;

            var cost = StandardCost ?? 0m; // Treat null or blank as 0
            return (ApprovedPriceDecimal - cost) / ApprovedPriceDecimal;
        }
    }

    // If you want a percentage instead, expose another property:
    public decimal? StandardMarginPercent =>
        StandardMargin * 100m;

    // Not mapped to DB; for display only
    public decimal NewPrice { get; set; }
    // New: parsed decimal version
    public decimal ApprovedPriceDecimal
    {
        get
        {
            decimal value;
            // TryParse avoids exceptions if string is blank or bad
            return decimal.TryParse(ApprovedPrice, out value) ? value : 0m;
        }
    }

    public string ItemStatusText
    {
        get
        {
            return ItemStatus switch
            {
                "A" => "Active",
                "O" => "Obsolete",
                "S" => "Slow Moving",
                _ => "Unknown"
            };
        }
    }
    public string PrivateLabelText =>
        PrivateLabelFlag == 1 ? "Yes" :
        PrivateLabelFlag == 0 ? "No" :
        "Unknown";

    public string CustomerPCFGroup
    {
        get
        {
            return $"Cust#: {CustomerNumber}  PCF#: {PCFNumber}";
        }
    }
    public string CustomerNamePCFGroup
    {
        get
        {
            return $"Cust#: {CustomerNumber} {CustomerName}     PCF#: {PCFNumber}";
        }
    }
    public string PCFStatusText => PCFStatus switch
    {
        0 => "New",
        1 => "Awaiting SM Approval",
        2 => "Awaiting VP Approval",
        3 => "Approved",
        -1 => "Reopened",
        99 => "Expired",
        _ => PCFStatus.ToString()
    };
    public string PcfTypeText => (PcfType ?? "").ToUpperInvariant() switch
    {
        "W" => "Warehouse (Standard)",
        "DS" => "Dropship (Standard)",
        "PW" => "Promo Warehouse",
        "PD" => "Promo Dropship",
        "T" => "Truckload",
        "PL" => "Private Label Only",
        "D" => "Direct",
        "PART" => "Parts Only",
        _ => PcfType ?? string.Empty
    };
}
