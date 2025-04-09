namespace BlazorServerDatagridApp2.Models;


// File: ItemStatus.cs
public enum PCFStatus
{
    New = 0,
    AwaitingSMApproval = 1,
    AwaitingVPApproval = 2,
    Approved = 3,
    Reopened = -1,
    Expired = 99,
    Replaced = 98
}

public static class ItemStatusExtensions
{
    public static string ToDescription(this PCFStatus status)
    {
        return status switch
        {
            PCFStatus.New => "New",
            PCFStatus.AwaitingSMApproval => "Awaiting SM Approval",
            PCFStatus.AwaitingVPApproval => "Awaiting VP Approval",
            PCFStatus.Approved => "Approved",
            PCFStatus.Reopened => "Reopened",
            PCFStatus.Expired => "Expired",
            PCFStatus.Replaced => "Replaced",
            _ => "Unknown"
        };
    }

    public static string ToIconPath(this PCFStatus status)
    {
        return status switch
        {
            PCFStatus.New => "/images/status_new.png",
            PCFStatus.AwaitingSMApproval => "/images/status_submitted.png",
            PCFStatus.AwaitingVPApproval => "/images/status_inProcess.png",
            PCFStatus.Approved => "/images/status_approved.png",
            PCFStatus.Expired => "/images/status_expired.png",
            _ => "/images/status_unknown.png"
        };
    }

    public static string ToFontAwesomeClass(this PCFStatus status)
    {
        return status switch
        {
            PCFStatus.New => "fa-regular fa-envelope",
            PCFStatus.AwaitingSMApproval => "fa-regular fa-user",
            PCFStatus.AwaitingVPApproval => "fa-solid fa-user-tie",
            PCFStatus.Approved => "fa-solid fa-square-check",
            PCFStatus.Reopened => "fa-regular fa-user-xmark",
            PCFStatus.Expired => "fa-regular fa-calendar-xmark",
            _ => "fa-regular fa-question-circle"
        };
    }

}