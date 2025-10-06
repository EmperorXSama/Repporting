namespace Reporting.lib.Models.DTO;

public class ProxyUpdateRegion
{
    [JsonPropertyName("proxyId")]
    public int ProxyId { get; set; }

    public string? YahooConnectivity { get; set; }
    public string? Region { get; set; }
    [JsonPropertyName("availability")] public bool Availability { get; set; }
}
 