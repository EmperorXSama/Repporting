namespace Reporting.lib.Models.Core;

public partial class CentralProxy : ObservableObject
{
    public bool IsChecked { get; set; }
    public string Ip { get; set; }
    public string Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    [ObservableProperty] private string _ms;
    [ObservableProperty] private string _googleConnectivity;
    [ObservableProperty] private string _yahooConnectivity;
    [ObservableProperty] private string _region;
    [ObservableProperty] private bool _availability;
    public CentralProxy(bool isChecked,string ip,string port,string username,string password,string ms,string googleConnectivdity,string yahooConnectivity,string region,bool availability)
    {
        IsChecked = isChecked;
        Ip = ip;
        Port = port;
        Username = username;
        Password = password;
        Ms = ms;
        GoogleConnectivity = googleConnectivdity;
        YahooConnectivity = yahooConnectivity;
        Region = region;
        Availability = availability;
        
    }
    
}