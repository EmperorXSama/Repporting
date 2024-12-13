using Reporting.lib.Models.Core;
using Reporting.lib.Models.DTO;

namespace ReportingApi.Models;

public class AddEmailsRequest
{
    public IEnumerable<CreateEmailAccountDto> EmailAccounts { get; set; } = new List<CreateEmailAccountDto>();
    public  Dictionary<string,EmailMetadataDto> emailMetadata { get; set; } = new  Dictionary<string,EmailMetadataDto>();
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
}