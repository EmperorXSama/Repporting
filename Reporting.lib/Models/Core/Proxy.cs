

using Reporting.lib.Models.Core;

namespace Reporting.lib.enums.Core;

public class Proxy
{
    public int Id { get; set; }
    public string ProxyIp { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public ICollection<EmailAccount> EmailAccounts { get; set; } = new List<EmailAccount>();
}