using Reporting.lib.Models.Core;
using Reporting.lib.Models.DTO;

namespace ReportingApi.Models;

public class AddEmailsRequest
{
    public IEnumerable<CreateEmailAccountDto> EmailAccounts { get; set; } = new List<CreateEmailAccountDto>();
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
}