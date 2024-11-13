using System.Text.Json.Serialization;

namespace Reporting.lib.Models.Core;

public class Process
{
    public int Id { get; set; }

    [JsonPropertyName("operationName")]
    public string OperationName { get; set; }

    public Guid GuidId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int EmailGroupId { get; set; }

    public EmailGroup EmailGroup { get; set; }

    [JsonPropertyName("logs")]
    public ICollection<ProcessLog> Logs { get; set; } = new List<ProcessLog>();

    [JsonPropertyName("errorLogs")]
    public ICollection<ErrorLog> ErrorLogs { get; set; } = new List<ErrorLog>();

    public bool IsCampaign { get; set; }
}
