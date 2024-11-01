namespace Reporting.lib.Models.Core;

public class ErrorLog
{
    public int Id { get; set; }
    public int ProcessId { get; set; }
    public Process Process { get; set; }
    public DateTime OccurredAt { get; set; }
    public string ErrorType { get; set; } // e.g., ProxyError, HttpError
    public string ErrorMessage { get; set; }
}