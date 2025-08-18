using PcfManager.Models;

namespace PcfManager.Services;

public class PaymentTermsService
{
    private List<PaymentTerm> terms;

    public PaymentTermsService(List<PaymentTerm> termsList)
    {
        terms = termsList;
    }

    // Get Description by Terms_Code
    public string GetDescriptionByCode(string termsCode)
    {
        return terms.FirstOrDefault(t => t.Terms_Code == termsCode)?.Description;
    }

    // Get Terms_Code by Description
    public string GetCodeByDescription(string description)
    {
        return terms.FirstOrDefault(t => t.Description == description)?.Terms_Code;
    }
}