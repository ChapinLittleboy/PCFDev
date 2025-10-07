namespace PcfManager.Models;


// Note:  table name is PCItems
public class PCFItemDTO
{
    public string PCFNumber { get; set; }
    public string ItemNum { get; set; }
    public string CustNum { get; set; }
    public string ItemDesc { get; set; }
    public double ProposedPrice { get; set; }
    public int AnnEstUnits { get; set; }
    public int AnnEstDollars { get; set; }
    public decimal LYPrice { get; set; }
    public int LYUnits { get; set; }


    public int ID { get; set; }

    public double PP1Price { get; set; }
    public double PP2Price { get; set; }
    public double BM1Price { get; set; }
    public double BM2Price { get; set; }
    public double ListPrice { get; set; }
    public double FOBPrice { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? Family_Code { get; set; }
    public string? Family_Code_Description { get; set; }

    public string? UserName { get; set; }  // set and used only in the update query

    public string? ItemStatus { get; set; }

    public decimal CurrentFYSales { get; set; }
    public int CurrentFYUnits { get; set; }
    public decimal Prior1FYSales { get; set; }
    public int Prior1FYUnits { get; set; }
    public decimal Prior2FYSales { get; set; }
    public int Prior2FYUnits { get; set; }
    public string CurrentFYName { get; private set; }
    public string Prior1FYName { get; private set; }
    public string Prior2FYName { get; private set; }

    public double StandardCost { get; set; }  // im.cur_u_cost
    public double Margin => ProposedPrice > 0
        ? (ProposedPrice - StandardCost) / ProposedPrice
        : 0;

    

    // Constructor
    public PCFItemDTO()
    {
        SetFiscalYearNames();
    }

    private void SetFiscalYearNames()
    {
        DateTime today = DateTime.Today;

        // Fiscal year ends on August 31
        int fiscalYear = (today.Month > 8) ? today.Year + 1 : today.Year;

        CurrentFYName = $"FY{fiscalYear}";
        Prior1FYName = $"FY{fiscalYear - 1}";
        Prior2FYName = $"FY{fiscalYear - 2}";
    }






}


