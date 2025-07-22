namespace ReportingApi.Models;

public class MoveEmailsRequest
{
    public string SourceGroupNames { get; set; } // This accepts comma-separated string
    public string DestinationGroupName { get; set; }
}