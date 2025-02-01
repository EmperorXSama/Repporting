namespace RepportingApp.CoreSystem.ApiSystem;

public class ApiEndPoints
{
    // private API 
    /*private static readonly string BaseUrl = "http://65.109.62.85:2030/api";*/
    private static readonly string BaseUrl = "http://localhost:5164/api";
    public static string GetGroups => $"{BaseUrl}/Group";
    public static string PostGroup => $"{BaseUrl}/Group/AddGroup";
    public static string PostProxy => $"{BaseUrl}/Proxy/UploadProxies";
    public static string DeleteGroup => $"{BaseUrl}/Group/DeleteGroup";
    public static string GetEmails => $"{BaseUrl}/Emails/GetEmails";
    public static string PostEmailsProxiesUpdate => $"{BaseUrl}/Emails/UpdateProxies";
    public static string GetAllProxies => $"{BaseUrl}/Proxy/GetProxies";
    public static string GetAddEmails => $"{BaseUrl}/Emails/AddEmails";
    public static string UpdateStatsEmails => $"{BaseUrl}/Emails/UpdateStats";
    
    
    
    
    // webShare api end points 
    private static readonly string _webShareBaseUrl = "https://proxy.webshare.io/api/v2/proxy/list";
    public static string GetReplacedProxies = $"{_webShareBaseUrl}/replaced/?page_size=100";
    
}