namespace DataAccess.Models;

public class ProxyModel
{
    public string Ip { get; set; }
    public string Port { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string Region { get; set; }
}
public class RegionProxyInfo
{
    public string Region { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}