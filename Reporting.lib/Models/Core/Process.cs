namespace Reporting.lib.Models.Core;

public class Process
{
    public int Id { get; set; }
    public string OperationName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int EmailGroupId { get; set; }
    public EmailGroup EmailGroup { get; set; }
    public ICollection<ProcessLog> Logs { get; set; } = new List<ProcessLog>();
    public ICollection<ErrorLog> ErrorLogs { get; set; } = new List<ErrorLog>();
}