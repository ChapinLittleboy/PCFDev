using FluentValidation;
using Moq;
namespace PCFProject.tests;

public class PCFStartDateValidationTests
{
    [Fact]
    public async Task StartDateValidation_ShouldFail_WhenCommonItemNumAndStartDateExists()
    {
        // Arrange
        var mockDataService = new Mock<IDataService>();
        mockDataService.Setup(ds => ds.GetPcfCustItemStartApproved(It.IsAny<string>()))
            .ReturnsAsync(new List<ExistingRecord>
            {
                new ExistingRecord { ItemNum = "Item1", Sdate = new DateTime(2024, 1, 1) },
                new ExistingRecord { ItemNum = "Item2", Sdate = new DateTime(2024, 2, 1) }
            });

        var validator = new PCFValidator(mockDataService.Object);

        var dto = new PCFDTO
        {
            CustomerNumber = "Cust123",
            StartDate = new DateTime(2024, 1, 1),
            PCFLines = new List<PCFLine>
            {
                new PCFLine { ItemNum = "Item1" },
                new PCFLine { ItemNum = "Item3" }
            }
        };

        // Act
        var validationResult = await validator.ValidateAsync(dto);

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Contains(validationResult.Errors, error => error.PropertyName == nameof(PCFDTO.StartDate) &&
                                                         error.ErrorMessage.Contains("Error: Existing PCF for this customer has same Start Date with at least one item in common."));
    }

    [Fact]
    public async Task StartDateValidation_ShouldPass_WhenNoCommonItemNumAndStartDateExists()
    {
        // Arrange
        var mockDataService = new Mock<IDataService>();
        mockDataService.Setup(ds => ds.GetPcfCustItemStartApproved(It.IsAny<string>()))
            .ReturnsAsync(new List<ExistingRecord>
            {
                new ExistingRecord { ItemNum = "Item2", Sdate = new DateTime(2024, 1, 1) },
                new ExistingRecord { ItemNum = "Item3", Sdate = new DateTime(2024, 2, 1) }
            });

        var validator = new PCFValidator(mockDataService.Object);

        var dto = new PCFDTO
        {
            CustomerNumber = "Cust123",
            StartDate = new DateTime(2024, 1, 1),
            PCFLines = new List<PCFLine>
            {
                new PCFLine { ItemNum = "Item1" }
            }
        };

        // Act
        var validationResult = await validator.ValidateAsync(dto);

        // Assert
        Assert.True(validationResult.IsValid);
    }
}
// Mocked Models
public class ExistingRecord
{
    public string ItemNum { get; set; }
    public DateTime Sdate { get; set; }
}

public class PCFDTO
{
    public string CustomerNumber { get; set; }
    public DateTime StartDate { get; set; }
    public List<PCFLine> PCFLines { get; set; }
}

public class PCFLine
{
    public string ItemNum { get; set; }
}

// Validator Implementation
public class PCFValidator : AbstractValidator<PCFDTO>
{
    public PCFValidator(IDataService dataService)
    {
        RuleFor(x => x.StartDate)
            .MustAsync(async (dto, startDate, cancellation) =>
            {
                var existingRecords = await dataService.GetPcfCustItemStartApproved(dto.CustomerNumber);
                return existingRecords.All(record =>
                    record.Sdate != startDate || !dto.PCFLines.Any(line => line.ItemNum == record.ItemNum));
            })
            .WithMessage("Error: Existing PCF for this customer has same Start Date with at least one item in common.");
    }
}

// Mocked Interface
public interface IDataService
{
    Task<List<ExistingRecord>> GetPcfCustItemStartApproved(string customerNumber);
}