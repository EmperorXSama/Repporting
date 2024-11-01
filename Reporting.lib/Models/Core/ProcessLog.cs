using Reporting.lib.enums.Core;

namespace Reporting.lib.Models.Core;

public class ProcessLog
{
    public int Id { get; set; }
    public int EmailAccountId { get; set; }
    public EmailAccount EmailAccount { get; set; }
    public int ProcessId { get; set; }
    public Process Process { get; set; }
    public DateTime ProcessedAt { get; set; }
    public ProcessResult Result { get; set; }
    public int? SpamCount { get; set; }
}