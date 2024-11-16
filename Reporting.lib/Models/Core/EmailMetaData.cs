using System.Text.Json.Serialization;

namespace Reporting.lib.Models.Core;

public class EmailMetaData
{
    [JsonPropertyName("metaDataId")] public int MetaDataId { get; set; }

    [JsonPropertyName("mailId")] public string MailId { get; set; }

    [JsonPropertyName("ymreqId")] public string YmreqId { get; set; }

    [JsonPropertyName("wssid")] public string Wssid { get; set; }

    [JsonPropertyName("cookie")] public string Cookie { get; set; }
}