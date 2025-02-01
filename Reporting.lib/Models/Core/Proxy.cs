using System.Text.Json.Serialization;

namespace Reporting.lib.Models.Core;

public partial class Proxy : ObservableObject
{
    public int ProxyId { get; set; }

    [JsonPropertyName("proxyIp")]
    public string ProxyIp { get; set; }

    public int Port { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }
    
    [ObservableProperty] private string _ms;
    [ObservableProperty] private string _googleConnectivity;
    [ObservableProperty] private string _yahooConnectivity;
    [ObservableProperty] private string _region;
    [ObservableProperty] private bool _availability;

    [JsonPropertyName("emailAccounts")]
    public ICollection<EmailAccount> EmailAccounts { get; set; } = new List<EmailAccount>();
    
}