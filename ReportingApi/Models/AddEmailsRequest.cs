using Reporting.lib.Models.Core;

namespace ReportingApi.Models;

public class AddEmailsRequest
{
    public IEnumerable<EmailAccount> EmailAccounts { get; set; } = new List<EmailAccount>();
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
}