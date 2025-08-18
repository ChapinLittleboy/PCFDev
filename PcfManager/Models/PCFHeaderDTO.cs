using PcfManager.Services;
using FluentValidation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PcfManager.Models;

public class PCFHeaderDTO : INotifyPropertyChanged
{
    #region Private Fields
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserService _userService;
    private string? _lastEditNotes;
    private DateTime _lastEditDate;
    private bool _lastEditNotesChanged = false;
    private bool _isLastEditNotesInitialized = false;
    #endregion

    #region Constructors
    // Parameterless constructor for Dapper
    public PCFHeaderDTO()
    {
    }
    #endregion

    #region Primary Identifiers
    public int PcfNum { get; set; }

    public string? PcfNumber
    {
        get => PcfNum.ToString();
        set
        {
            if (int.TryParse(value, out int result))
            {
                PcfNum = result;
            }
        }
    }

    public string? DisplayNumAndDates => $"PCF {PcfNumber} Dates {StartDate:MM-dd-yyyy} to {EndDate:MM-dd-yyyy}";
    #endregion

    #region Customer Information
    [Required(ErrorMessage = "Customer Number is required.")]
    public string? CustomerNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Name is required.")]
    public string? CustomerName { get; set; } = string.Empty;

    public string? BuyingGroup { get; set; } = string.Empty;

    public Customer CustomerInfo { get; set; }

    public string? CustContact { get; set; }
    public string? CustContactEmail { get; set; }
    #endregion

    #region Billing Information
    public string? BillToAddress { get; set; } = string.Empty;
    public string? BillToCity { get; set; } = string.Empty;
    public string? BTState { get; set; } = string.Empty;
    public string? BTZip { get; set; } = string.Empty;
    public string? BillToCountry { get; set; } = string.Empty;
    public string? BillToPhone { get; set; } = string.Empty;
    #endregion

    #region Date Information
    public DateTime DateEntered { get; set; }

    [Required(ErrorMessage = "Start Date is required.")]
    [DateRangeValidation]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End Date is required.")]
    public DateTime EndDate { get; set; }

    public DateTime? LastEditDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public DateTime SubmittedDate { get; set; }
    #endregion

    #region PCF Configuration
    public string? PcfType { get; set; }

    public string PCFTypeDescription
    {
        get
        {
            // If the dictionary contains your code, return the matching description
            if (PcfTypeDescriptions.TryGetValue(PcfType, out string description))
            {
                return description;
            }
            // Otherwise, return something default or empty
            return "Unknown";
        }
    }

    public string? MarketType
    {
        get => EUT;
        set => EUT = value;
    }

    public string? EUT { get; set; }
    public string? CmaRef { get; set; }
    #endregion

    #region Payment and Shipping Terms
    public List<PaymentTerm> PaymentTermsList { get; set; } // Inject the list
    public string PromoPaymentTermsDescription =>
        PaymentTermsList?.FirstOrDefault(t => t.Terms_Code == PromoPaymentTerms)?.Description ?? "";

    public string? StandardPaymentTermsType { get; set; }  // This is the Terms code
    public string StandardPaymentTermsDescription =>
        PaymentTermsList?.FirstOrDefault(t => t.Terms_Code == StandardPaymentTermsType)?.Description ?? "";

    public string? PromoPaymentTerms { get; set; }
    public string? PromoPaymentTermsText { get; set; }
    public string? PromoFreightTerms { get; set; }
    public string? PromoFreightMinimums { get; set; }
    public string? FreightTerms { get; set; }
    public string? FreightMinimums { get; set; }
    #endregion

    #region Representative Information
    [Required(ErrorMessage = "Rep ID is required.")]
    [StringLength(10, ErrorMessage = "Rep ID cannot be longer than 10 characters.")]
    public string? RepID { get; set; } = string.Empty;

    public string? RepName { get; set; } = string.Empty;
    public string? RepEmail { get; set; } = string.Empty;
    public string? RepAgency { get; set; } = string.Empty;
    public string? RepPhone { get; set; } = string.Empty;
    #endregion

    #region User and Buyer Information
    public string LoggedInUser { get; set; }
    public string? Buyer { get; set; }
    public string? BuyerEmail { get; set; }
    public string? BuyerPhone { get; set; }
    #endregion

    #region Notes and Edit Information
    public string? GeneralNotes { get; set; }

    public string? LastEditNotes
    {
        get => _lastEditNotes;
        set
        {
            // Skip prepending if the field is being initialized
            if (!_isLastEditNotesInitialized && _lastEditNotes == null)
            {
                _lastEditNotes = value;
                _isLastEditNotesInitialized = true;
            }
            else
            {
                bool hasChanged = !((string.IsNullOrEmpty(_lastEditNotes) && string.IsNullOrEmpty(value)) ||
                                    _lastEditNotes == value);
                if (hasChanged)
                {
                    // Format the user and date information
                    string currentDate = DateTime.Now.ToString("MM/dd/yyyy"); // Format the date

                    // Prepend the information to the new value
                    // Check if the current date and user are already part of _lastEditNotes
                    string prefix = $"{currentDate} ({LoggedInUser}) :";
                    if (value != null && !value.StartsWith(prefix))
                    {
                        // Prepend the information to the new value
                        _lastEditNotes = $"{prefix} {value}";
                    }

                    LastEditDate = DateTime.Now; // Automatically update
                    LastEditedBy = LoggedInUser; // Automatically update
                    LastEditNotesChanged = true; // Mark this field as changed
                    OnPropertyChanged(nameof(LastEditNotes));
                }
            }
        }
    }

    public bool LastEditNotesChanged
    {
        get => _lastEditNotesChanged; // Expose the tracking field as a public property
        private set => _lastEditNotesChanged = value; // Only settable within the class
    }

    public string? LastEditedBy { get; set; }
    public string? LastUpdatedBy { get; set; }
    #endregion

    #region Approval Information
    public string? ApprovalStatusNotes { get; set; }
    public PCFStatus ApprovalStatus { get; set; }

    public int? PCFStatus { get; set; } // 0 = New, 1 = Awaiting SM Approval, 2 = Awaiting VP Approval, 3 = Approved, -1 = Reopened, 99 = Expired

    public int Approved { get; set; }

    public string? SubmitterEmail { get; set; }
    public string? SubmittedBy { get; set; }

    public string? Salesman { get; set; }
    public string? SalesManager { get; set; }

    public string? SalesMngrApproval { get; set; }
    public DateTime? SalesMngrDate { get; set; }

    public string? VPSalesApprovl { get; set; }
    public DateTime? VPSalesDate { get; set; }
    #endregion

    #region Related Collections
    // Navigation property for details
    public List<PCFItemDTO> PCFLines { get; set; } = new();
    #endregion

    #region Constants and Static Data
    private static readonly Dictionary<string, string> PcfTypeDescriptions = new Dictionary<string, string>
    {
        { "W",   "Warehouse (Standard)" },
        { "DS",  "Dropship (Standard)" },
        { "PW",  "Promo Warehouse" },
        { "PD",  "Promo Dropship" },
        { "T",   "Truckload" },
        { "PL",  "Private Label Only" },
        { "D",   "Direct" },
        { "PART","Parts Only" }
    };
    #endregion

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    #region Helper Methods
    public void ResetEditNotesTracking()
    {
        LastEditNotesChanged = false;
    }
    #endregion
}

public class PCFHeaderDTOxxx : INotifyPropertyChanged
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserService _userService;


    private string? _lastEditNotes;
    private DateTime _lastEditDate;


    // Parameterless constructor for Dapper
    public PCFHeaderDTOxxx()
    {
    }


    public int PcfNum { get; set; }

    public string LoggedInUser { get; set; }


    [Required(ErrorMessage = "Customer Number is required.")]
    public string? CustomerNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Name is required.")]
    public string? CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rep ID is required.")]
    [StringLength(10, ErrorMessage = "Rep ID cannot be longer than 10 characters.")]
    public string? RepID { get; set; } = string.Empty;

    public string? RepName { get; set; } = string.Empty;
    public string? RepEmail { get; set; } = string.Empty;
    public string? RepAgency { get; set; } = string.Empty;
    public string? RepPhone { get; set; } = string.Empty;

    public DateTime DateEntered { get; set; }
    public string? BuyingGroup { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start Date is required.")]
    [DateRangeValidation]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End Date is required.")]
    public DateTime EndDate { get; set; }

    public string? BillToAddress { get; set; } = string.Empty;
    public string? BillToCity { get; set; } = string.Empty;
    public string? BillToCountry { get; set; } = string.Empty;
    public string? BillToPhone { get; set; } = string.Empty;
    public string? BTState { get; set; } = string.Empty;
    public string? BTZip { get; set; } = string.Empty;
    public string? Buyer { get; set; }

    public string? BuyerEmail { get; set; }

    public string? BuyerPhone { get; set; }

    public string? PcfType { get; set; }
    public string PCFTypeDescription
    {
        get
        {
            // If the dictionary contains your code, return the matching description
            if (PcfTypeDescriptions.TryGetValue(PcfType, out string description))
            {
                return description;
            }
            // Otherwise, return something default or empty
            return "Unknown";
        }
    }

    public List<PaymentTerm> PaymentTermsList { get; set; } // Inject the list

    public string? StandardPaymentTermsType { get; set; }

    public string? PromoPaymentTerms { get; set; }
    public string? PromoPaymentTermsText { get; set; }

    public string? FreightTerms { get; set; }

    public string? FreightMinimums { get; set; }
    public string? GeneralNotes { get; set; }
    public string? ApprovalStatusNotes { get; set; }
    public string? CmaRef { get; set; }
    private bool _lastEditNotesChanged = false; // Tracks if LastEditNotes has changed
    private bool _isLastEditNotesInitialized = false;


    public string? LastEditedBy { get; set; }

    public string? LastEditNotes
    {
        get => _lastEditNotes;
        set
        {
            // Skip prepending if the field is being initialized
            if (!_isLastEditNotesInitialized && _lastEditNotes == null)
            {
                _lastEditNotes = value;
                _isLastEditNotesInitialized = true;
            }
            else
            {
                bool hasChanged = !((string.IsNullOrEmpty(_lastEditNotes) && string.IsNullOrEmpty(value)) ||
                                    _lastEditNotes == value);
                if (hasChanged)
                {
                    // Format the user and date information
                    //string user = UserService.UserName ?? "Unknown User"; // Handle null case for safety
                    string currentDate = DateTime.Now.ToString("MM/dd/yyyy"); // Format the date

                    // Prepend the information to the new value
                    // Check if the current date and user are already part of _lastEditNotes
                    string prefix = $"{currentDate} ({LoggedInUser}) :";
                    if (value != null && !value.StartsWith(prefix))
                    {
                        // Prepend the information to the new value
                        _lastEditNotes = $"{prefix} {value}";
                    }

                    LastEditDate = DateTime.Now; // Automatically update
                    LastEditedBy = LoggedInUser; // Automatically update
                    LastEditNotesChanged = true; // Mark this field as changed
                    OnPropertyChanged(nameof(LastEditNotes));
                }
            }
        }
    }


    public bool LastEditNotesChanged
    {
        get => _lastEditNotesChanged; // Expose the tracking field as a public property
        private set => _lastEditNotesChanged = value; // Only settable within the class
    }

    public DateTime? LastEditDate { get; set; }


    public string? Salesman { get; set; }
    public string? SalesManager { get; set; }


    public string? SubmitterEmail { get; set; }

    public string? SubmittedBy { get; set; }
    public DateTime SubmittedDate { get; set; }
    public PCFStatus ApprovalStatus { get; set; }
    public string? EUT { get; set; }

    public string? CustContact { get; set; }
    public string? CustContactEmail { get; set; }

    public int?
        PCFStatus
    {
        get;
        set;
    } // 0 = New, 1 = Awaiting SM Approval, 2 = Awaiting VP Approval, 3 = Approved, -1 = Reopened, 99 = Expired


    public string? SalesMngrApproval { get; set; }
    public DateTime? SalesMngrDate { get; set; }


    public string? VPSalesApprovl { get; set; }
    public DateTime? VPSalesDate { get; set; }
    public int Approved { get; set; }

    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedDate { get; set; }

    public string? MarketType
    {
        get => EUT;
        set => EUT = value;
    }

    public string? PcfNumber
    {
        get => PcfNum.ToString();
        set
        {
            if (int.TryParse(value, out int result))
            {
                PcfNum = result;
            }
        }
    }

    public Customer CustomerInfo { get; set; }


    // Navigation property for details
    public List<PCFItemDTO> PCFLines { get; set; } = new();

    public string? DisplayNumAndDates => $"PCF {PcfNumber} Dates {StartDate:MM-dd-yyyy} to {EndDate:MM-dd-yyyy}";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void ResetEditNotesTracking()
    {
        LastEditNotesChanged = false;
    }


    private static readonly Dictionary<string, string> PcfTypeDescriptions = new Dictionary<string, string>
    {
        { "W",   "Warehouse (Standard)" },
        { "DS",  "Dropship (Standard)" },
        { "PW",  "Promo Warehouse" },
        { "PD",  "Promo Dropship" },
        { "T",   "Truckload" },
        { "PL",  "Private Label Only" },
        { "D",   "Direct" },
        { "PART","Parts Only" }
    };

}


public class DateRangeValidation : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var dto = (PCFHeaderDTO)validationContext.ObjectInstance;

        // Validate only when both dates are provided
        if (dto.StartDate != default && dto.EndDate != default)
        {
            if (dto.StartDate > dto.EndDate)
            {
                return new ValidationResult("Start Date cannot be later than End Date.");
            }
        }

        return ValidationResult.Success;
    }
}


public class PCFHeaderDTOValidator : AbstractValidator<PCFHeaderDTO>
{

    public PCFHeaderDTOValidator(DataService _dataService)
    {



        RuleFor(x => x.CustomerNumber)
            .Cascade(CascadeMode.Continue).NotEmpty().WithMessage("Customer Number is required.");
        RuleFor(x => x.StartDate)
            .Cascade(CascadeMode.Continue).NotEmpty().WithMessage("Start Date is required.")
            .LessThan(x => x.EndDate).WithMessage("Start Date must be earlier than End Date.");
        RuleFor(x => x.EndDate)
            .Cascade(CascadeMode.Continue).NotEmpty().WithMessage("End Date is required.");
        RuleFor(x => x.PCFStatus)
            .Cascade(CascadeMode.Continue).NotEmpty().WithMessage("PCF Status is required.");
        RuleFor(x => x.PcfType)
            .Cascade(CascadeMode.Continue).NotEmpty().WithMessage("PCF Type is required.");

        RuleFor(x => x.PromoPaymentTerms)
            .Cascade(CascadeMode.Continue).NotEmpty().When(x => x.PcfType == "PD" || x.PcfType == "PW").WithMessage("Promo Terms is required.");
        RuleFor(x => x.FreightTerms)
            .Cascade(CascadeMode.Continue).NotEmpty().When(x => x.PcfType == "PD" || x.PcfType == "PW").WithMessage("Promo Freight Terms is required.");
        RuleFor(x => x.FreightMinimums)
            .Cascade(CascadeMode.Continue).NotEmpty().When(x => x.PcfType == "PD" || x.PcfType == "PW").WithMessage("Promo Freight Minimums is required.");



        RuleFor(x => x.Buyer)
            .Cascade(CascadeMode.Continue).NotEmpty().WithMessage("Buyer Name is required.");
        RuleFor(x => x.BuyerEmail)
            .Cascade(CascadeMode.Continue).NotEmpty().WithMessage("Buyer Email is required.")
            .EmailAddress().WithMessage("Invalid email format");


        RuleFor(x => x.RepID)
            .Cascade(CascadeMode.Continue).NotEmpty().WithMessage("Rep ID is required.")
            .MaximumLength(10).WithMessage("Rep ID cannot be longer than 10 characters.");
        RuleForEach(x => x.PCFLines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProposedPrice)
                .GreaterThan(0)
                .WithMessage(l => $"Accepted Price for Item {l.ItemNum} must be greater than 0");
        });
        RuleFor(x => x.StartDate)
            .Cascade(CascadeMode.Continue).MustAsync(async (dto, startDate, cancellation) =>
            {
                //var existingRecords = await _dataService.GetPcfCustItemStartApproved(dto.CustomerNumber);
                // Fetch existing records for the customer, excluding the current PCF
                var existingRecords = (await _dataService.GetPcfCustItemStartApproved(dto.CustomerNumber))
                    .Where(record => record.PcfNum != dto.PcfNum.ToString()); // Exclude the current PCF

                // Check if there is a common ItemNum and the same StartDate
                return existingRecords.All(record =>
                    record.Sdate != startDate ||
                    !dto.PCFLines.Any(line => line.ItemNum == record.ItemNum));
            })
            .WithMessage("Error: Existing PCF for this customer has same Start Date with at least one item in common.");


    }
}


