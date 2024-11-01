using Reporting.lib.enums.Core;

namespace Reporting.lib.Models.Core;

public class EmailAccount
{
    public int Id { get; set; }
    public string EmailAddress { get; set; }
    public string Password { get; set; }
    public string RecoveryEmail { get; set; }
    public Proxy Proxy { get; set; }
    public EmailStatus Status { get; set; }
    public DateTime? FirstUse { get; set; }
    public DateTime? LastUse { get; set; }
    public int GroupId { get; set; }
    public EmailGroup Group { get; set; }
    public ICollection<ProcessLog> ProcessLogs { get; set; } = new List<ProcessLog>();
}