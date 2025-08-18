namespace PcfManager.Models;

public class PcfAuditLog
{
    public int Id { get; set; } // Unique identifier for the change log entry
    public string TableName { get; set; } // Name of the table where the change occurred
    public string RecordKey { get; set; } // Primary key or identifier for the changed record
    public string FieldName { get; set; } // Name of the field that was changed
    public string? OldValue { get; set; } // Value of the field before the change
    public string? NewValue { get; set; } // Value of the field after the change
    public string ChangedBy { get; set; } // User who made the change
    public DateTime ChangedDate { get; set; } // Date and time of the change
}

