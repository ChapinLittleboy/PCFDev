namespace BlazorServerDatagridApp2.Data;
using AutoMapper;
using BlazorServerDatagridApp2.Models;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Define mappings between entity and DTO

        // Entity to DTO Mapping
        CreateMap<PCFHeaderEntity, PCFHeaderDTO>()
            .ForMember(dest => dest.PcfNumber, opt => opt.MapFrom(src => src.PCFNum.ToString()))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.ProgSDate))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.ProgEDate))
            .ForMember(dest => dest.BillToAddress, opt => opt.MapFrom(src => src.BTAddr))
            .ForMember(dest => dest.BillToCity, opt => opt.MapFrom(src => src.BTCity))
            .ForMember(dest => dest.BillToCountry, opt => opt.Ignore()) // Example of ignoring a property
            .ForMember(dest => dest.BillToPhone, opt => opt.MapFrom(src => src.BTPhone))
            .ForMember(dest => dest.BTState, opt => opt.MapFrom(src => src.BTState))
            .ForMember(dest => dest.BTZip, opt => opt.MapFrom(src => src.BTZip))
            .ForMember(dest => dest.GeneralNotes, opt => opt.MapFrom(src => src.GenNotes))
            .ForMember(dest => dest.LastEditedBy, opt => opt.MapFrom(src => src.EditBy))
            .ForMember(dest => dest.LastEditNotes, opt => opt.MapFrom(src => src.EditNotes))
            .ForMember(dest => dest.StandardPaymentTermsType, opt => opt.MapFrom(src => src.Standard_Terms))
            .ForMember(dest => dest.PromoPaymentTerms, opt => opt.MapFrom(src => src.Promo_Terms))
            .ForMember(dest => dest.PromoPaymentTermsText, opt => opt.MapFrom(src => src.Promo_Terms_Text))
            .ForMember(dest => dest.FreightTerms, opt => opt.MapFrom(src => src.Standard_Freight_Terms))
            .ForMember(dest => dest.PromoFreightTerms, opt => opt.MapFrom(src => src.Standard_Freight_Terms))

            .ForMember(dest => dest.FreightMinimums, opt => opt.MapFrom(src =>
                src.Freight_Minimums == "Other" ? src.Other_Freight_Minimums : src.Freight_Minimums))
            .ForMember(dest => dest.PromoFreightMinimums, opt => opt.MapFrom(src =>
                src.Freight_Minimums == "Other" && !string.IsNullOrWhiteSpace(src.Other_Freight_Minimums)
                    ? src.Other_Freight_Minimums
                    : src.Freight_Minimums))

            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.CustName))
            .ForMember(dest => dest.CustomerNumber, opt => opt.MapFrom(src => src.CustNum))
            .ForMember(dest => dest.LastEditDate, opt =>
                opt.MapFrom(src => src.EditDate == DateTime.MinValue ? DateTime.Now : src.EditDate))
            .ForMember(dest => dest.RepID, opt => opt.MapFrom(src => src.SRNum.ToUpper()))
            .ForMember(dest => dest.SubmitterEmail, opt => opt.MapFrom(src => src.SubmitterEmail))
            .ForMember(dest => dest.CustContactEmail, opt => opt.MapFrom(src => src.Email));



        // DTO to Entity Mapping

        // DTO to Entity Mapping
        CreateMap<PCFHeaderDTO, PCFHeaderEntity>()
            .ForMember(dest => dest.PCFNum, opt => opt.MapFrom(src => int.Parse(src.PcfNumber)))
            .ForMember(dest => dest.ProgSDate, opt => opt.MapFrom(src => src.StartDate))
            .ForMember(dest => dest.ProgEDate, opt => opt.MapFrom(src => src.EndDate))
            .ForMember(dest => dest.BTAddr, opt => opt.MapFrom(src => src.BillToAddress))
            .ForMember(dest => dest.BTCity, opt => opt.MapFrom(src => src.BillToCity))
            .ForMember(dest => dest.BTState, opt => opt.MapFrom(src => src.BTState))
            .ForMember(dest => dest.BTZip, opt => opt.MapFrom(src => src.BTZip)) // Added to match the mapping
            .ForMember(dest => dest.BTPhone, opt => opt.MapFrom(src => src.BillToPhone))
            .ForMember(dest => dest.GenNotes, opt => opt.MapFrom(src => src.GeneralNotes))
            .ForMember(dest => dest.EditBy, opt => opt.MapFrom(src => src.LastEditedBy))
            .ForMember(dest => dest.EditNotes, opt => opt.MapFrom(src => src.LastEditNotes))
            .ForMember(dest => dest.Standard_Terms, opt => opt.MapFrom(src => src.StandardPaymentTermsType))
            .ForMember(dest => dest.Promo_Terms, opt => opt.MapFrom(src => src.PromoPaymentTerms))
            .ForMember(dest => dest.Promo_Terms_Text, opt => opt.MapFrom(src => src.PromoPaymentTermsText)) // Added to match the mapping
            .ForMember(dest => dest.Standard_Freight_Terms, opt => opt.MapFrom(src => src.FreightTerms))
            .ForMember(dest => dest.Freight_Minimums, opt => opt.MapFrom(src => src.FreightMinimums))
            .ForMember(dest => dest.CustName, opt => opt.MapFrom(src => src.CustomerName)) // Added to match the mapping
            .ForMember(dest => dest.CustNum, opt => opt.MapFrom(src => src.CustomerNumber)) // Added to match the mapping
            .ForMember(dest => dest.SRNum, opt => opt.MapFrom(src => src.RepID)) // Added to match the mapping
            .ForMember(dest => dest.EditDate, opt => opt.MapFrom(src => src.LastEditDate));







        CreateMap<PCFItemEntity, PCFItemDTO>()
            .ForMember(dest => dest.PCFNumber, opt => opt.MapFrom(src => src.PCFNumber)) // Explicit for clarity
            .ForMember(dest => dest.ItemNum, opt => opt.MapFrom(src => src.ItemNum))
            .ForMember(dest => dest.CustNum, opt => opt.MapFrom(src => src.CustNum))
            .ForMember(dest => dest.ItemDesc, opt => opt.MapFrom(src => src.ItemDesc))
            .ForMember(dest => dest.ProposedPrice, opt => opt.MapFrom(src => src.ProposedPrice))
            .ForMember(dest => dest.AnnEstUnits, opt => opt.MapFrom(src => src.AnnEstUnits))
            .ForMember(dest => dest.AnnEstDollars, opt => opt.MapFrom(src => src.AnnEstDollars))
            .ForMember(dest => dest.LYPrice, opt => opt.MapFrom(src => src.LYPrice))
            .ForMember(dest => dest.LYUnits, opt => opt.MapFrom(src => src.LYUnits))
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID));

        // PCFItemDTO to PCFItemEntity Mapping
        CreateMap<PCFItemDTO, PCFItemEntity>()
            .ForMember(dest => dest.PCFNumber, opt => opt.MapFrom(src => src.PCFNumber)) // Explicit for clarity
            .ForMember(dest => dest.ItemNum, opt => opt.MapFrom(src => src.ItemNum))
            .ForMember(dest => dest.CustNum, opt => opt.MapFrom(src => src.CustNum))
            .ForMember(dest => dest.ItemDesc, opt => opt.MapFrom(src => src.ItemDesc))
            .ForMember(dest => dest.ProposedPrice, opt => opt.MapFrom(src => src.ProposedPrice))
            .ForMember(dest => dest.AnnEstUnits, opt => opt.MapFrom(src => src.AnnEstUnits))
            .ForMember(dest => dest.AnnEstDollars, opt => opt.MapFrom(src => src.AnnEstDollars))
            .ForMember(dest => dest.LYPrice, opt => opt.MapFrom(src => src.LYPrice))
            .ForMember(dest => dest.LYUnits, opt => opt.MapFrom(src => src.LYUnits))
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID));


        // Map UserHierarchy to UserHierarchyDTO
        CreateMap<UserHierarchy, UserHierarchyDTO>()
            .ForMember(dest => dest.ManagerDomainId, opt => opt.MapFrom(src => src.Manager.DomainId))
            .ForMember(dest => dest.SubordinateDomainId, opt => opt.MapFrom(src => src.Subordinate.DomainId));

        // Map UserHierarchyDTO to UserHierarchy
        CreateMap<UserHierarchyDTO, UserHierarchy>()
            .ForMember(dest => dest.ManagerId, opt => opt.Ignore()) // ManagerId will be resolved manually
            .ForMember(dest => dest.SubordinateId, opt => opt.Ignore()) // SubordinateId will be resolved manually
            .ForMember(dest => dest.Manager, opt => opt.Ignore())
            .ForMember(dest => dest.Subordinate, opt => opt.Ignore());
    }
}
