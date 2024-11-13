using System.Text.Json.Serialization;

namespace Reporting.lib.Models.Core;

public class ErrorLog
{
    public int Id { get; set; }

    public int ProcessId { get; set; }

    [JsonPropertyName("process")]
    public Process Process { get; set; } // Modify as needed

    public DateTime OccurredAt { get; set; }

    public string ErrorType { get; set; }

    public string ErrorMessage { get; set; }
}