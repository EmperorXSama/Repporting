using System.Text.Json.Serialization;
using Reporting.lib.enums.Core;

namespace Reporting.lib.Models.Core;


public class EmailAccount
{
    public int Id { get; set; }

    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("recoveryEmail")]
    public string RecoveryEmail { get; set; }

    [JsonPropertyName("proxy")]
    public Proxy Proxy { get; set; }

    [JsonPropertyName("status")]
    public EmailStatus Status { get; set; }

    [JsonPropertyName("firstUse")]
    public DateTime? FirstUse { get; set; }

    [JsonPropertyName("lastUse")]
    public DateTime? LastUse { get; set; }

    [JsonPropertyName("groupId")]
    public int? GroupId { get; set; }

    [JsonPropertyName("group")]
    public EmailGroup Group { get; set; }

    [JsonPropertyName("processLogs")]
    public ICollection<ProcessLog> ProcessLogs { get; set; } = new List<ProcessLog>();
}