namespace ReportingApi.Models;

public class MoveEmailsResult
{
    public int EmailsMovedCount { get; set; }
    public string SourceGroups { get; set; } // Comma-separated processed groups
    public string DestinationGroup { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public string NotFoundGroups { get; set; } // Groups that weren't found
}