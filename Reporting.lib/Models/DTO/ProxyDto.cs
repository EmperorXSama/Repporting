using System.Text.Json.Serialization;

namespace Reporting.lib.Models.DTO;

public class ProxyDto
{
    [JsonPropertyName("proxyId")]
    public int ProxyId { get; set; }

    [JsonPropertyName("proxyIp")]
    public string ProxyIp { get; set; }

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; } 
    [JsonPropertyName("availability")]
    public string Availability { get; set; }
    
}
public class ProxyUpdateDto
{
    public string OldProxyIp { get; set; }
    public int OldProxyPort { get; set; }
    public string NewProxyIp { get; set; }
    public int NewProxyPort { get; set; }
    public string? NewUsername { get; set; }
    public string? NewPassword { get; set; }
}
