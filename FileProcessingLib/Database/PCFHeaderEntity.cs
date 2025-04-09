namespace FileProcessingLib.Database;

public class PCFHeaderEntity
{
    public int PCFNum { get; set; }
    public string PcfNumber => PCFNum.ToString();  // for linking to PCItems
    public DateTime Date { get; set; }
    public string Warehouse { get; set; }
    public string Dropship { get; set; }
    public string OtherDropship { get; set; }
    public string OtherWarehouse { get; set; }
    public string DWOther { get; set; }
    public string DWOtherText { get; set; }
    public DateTime ProgSDate { get; set; }
    public DateTime ProgEDate { get; set; }
    public string CustNum { get; set; }
    public string CustName { get; set; }
    public string BTName { get; set; }
    public string BTAddr { get; set; }
    public string BTCity { get; set; }
    public string BTState { get; set; }
    public string BTZip { get; set; }
    public string BTPhone { get; set; }
    public string BTFax { get; set; }
    public string BTFaxPerm { get; set; }
    public string Buyer { get; set; }
    public string RepName { get; set; }
    public string RepEmail { get; set; }
    public string RepAgency { get; set; }
    public string RepPhone { get; set; }
    public string OtherTerms { get; set; }
    public string STName { get; set; }
    public string STAddr { get; set; }
    public string STCity { get; set; }
    public string STState { get; set; }
    public string STZip { get; set; }
    public string STPhone { get; set; }
    public string CustContact { get; set; }
    public string Email { get; set; }
    public string BuyingGroup { get; set; }
    public string OtherDating { get; set; }
    public string OtherDatingApprvl { get; set; }
    public string FtPickUpAllow { get; set; }
    public string FtDSPPD { get; set; }
    public string FtDSDollars { get; set; }
    public string GenNotes { get; set; }
    public string NSShipNotes { get; set; }
    public string RoutingNotes { get; set; }
    public string AdPercSales { get; set; }
    public string AdCMemo { get; set; }
    public string AdDFI { get; set; }
    public string AdPdQuarterly { get; set; }
    public string AdAnnually { get; set; }
    public string AdSemiAnnually { get; set; }
    public string AdMonthly { get; set; }
    public string AdPOP { get; set; }
    public string AdNotes { get; set; }
    public string VolPercSales { get; set; }
    public string VolCMemo { get; set; }
    public string VolDFI { get; set; }
    public string VolPaidQuarterly { get; set; }
    public string VolAnnually { get; set; }
    public string VolSemiAnnually { get; set; }
    public string VolMonthly { get; set; }
    public string VolNotes { get; set; }
    public string DefectiveDFI { get; set; }
    public string ActualDefectives { get; set; }
    public string DefectiveIssueCM { get; set; }
    public string CoopOtherNotes { get; set; }
    public string SubmittedBy { get; set; }
    public DateTime SubmitDate { get; set; }
    public string SalesMngrApproval { get; set; }
    public DateTime SalesMngrDate { get; set; }
    public string VPSalesApprovl { get; set; }
    public DateTime VPSalesDate { get; set; }
    public int Approved { get; set; }
    public string FtPUAllocPerc { get; set; }
    public string SoftwareNotes { get; set; }
    public string SRNum { get; set; }
    public string MngrComments { get; set; }
    public string ErrorMsg { get; set; }
    public string OtherMsg { get; set; }
    public string ItemComments { get; set; }
    public string EditBy { get; set; }
    public DateTime EditDate { get; set; }
    public string EditNotes { get; set; }
    public string EUT { get; set; }
    public int OrigPCF { get; set; }
    public int Tariff { get; set; }
    public string Standard_Terms { get; set; }
    public string Standard_Terms_Text { get; set; }
    public string Promo_Terms { get; set; }
    public string Promo_Terms_Text { get; set; }
    public string Standard_Freight_Terms { get; set; }
    public string Freight_Minimums { get; set; }
    public string Other_Freight_Minimums { get; set; }

    public string DisplayNumAndDates => $"PCF {PcfNumber} Dates {ProgSDate:MM-dd-yyyy} to {ProgEDate:MM-dd-yyyy}";

    public List<PCFItemEntity> PCFItems { get; set; }

}