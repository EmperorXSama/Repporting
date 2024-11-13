

using System.Text.Json.Serialization;
using Reporting.lib.Models.Core;

namespace Reporting.lib.enums.Core;

public class Proxy
{
    public int Id { get; set; }

    [JsonPropertyName("proxyIp")]
    public string ProxyIp { get; set; }

    public int Port { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    [JsonPropertyName("emailAccounts")]
    public ICollection<EmailAccount> EmailAccounts { get; set; } = new List<EmailAccount>();
}