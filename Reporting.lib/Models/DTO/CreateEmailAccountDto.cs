using System.Text.Json.Serialization;
using Reporting.lib.enums.Core;

namespace Reporting.lib.Models.DTO;

public class CreateEmailAccountDto
{
    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("recoveryEmail")]
    public string RecoveryEmail { get; set; }

    [JsonPropertyName("proxy")]
    public ProxyDto Proxy { get; set; }

    [JsonPropertyName("status")]
    public EmailStatus Status { get; set; }

    [JsonPropertyName("group")]
    public EmailGroup Group { get; set; }  

}
public class EmailMetadataDto
{
    public string MailId { get; set; }
    public string YmreqId { get; set; }
    public string Wssid { get; set; }
    public string Cookie { get; set; }
}
