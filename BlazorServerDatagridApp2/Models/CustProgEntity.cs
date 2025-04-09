namespace BlazorServerDatagridApp2.Models;

public class CustProgEntity
{
    public string? CustNum { get; set; }
    public int CustSeq { get; set; }
    public string? AllowanceType { get; set; }
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public string? Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? DiscountBasis { get; set; }
    public string? CreditMemoEmail { get; set; }
    public string? FreightTerms { get; set; }
    public short PRILOD { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime RecordDate { get; set; }
    public Guid RowPointer { get; set; }
    public byte NoteExistsFlag { get; set; }
    public byte InWorkflow { get; set; }
    public string? Uf_ProgNotes { get; set; }
    public int Uf_ProgGroupInd { get; set; }
    public string? Uf_ProgOtherNotes { get; set; }
    public int Uf_ProgArchive { get; set; }
    public string? Uf_FreightRate { get; set; }
    public string? Uf_GrossNet { get; set; }
    public string? Uf_FixVar { get; set; }
    public DateTime Uf_ContractEnd { get; set; }
    public DateTime Uf_ContractStart { get; set; }
    public string? Uf_PolicyA { get; set; }
    public decimal Uf_ProgTier1 { get; set; }
    public decimal Uf_ProgTier2 { get; set; }
    public decimal Uf_ProgTier3 { get; set; }
    public decimal Uf_ProgTier4 { get; set; }
    public string? Uf_TierLabel1 { get; set; }
    public string? Uf_TierLabel2 { get; set; }
    public string? Uf_TierLabel3 { get; set; }
    public string? Uf_TierLabel4 { get; set; }
    public int Uf_ContractAttached { get; set; }
}

