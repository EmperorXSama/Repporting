namespace RepportingApp.CoreSystem.ApiSystem;

public class ApiEndPoints
{
    // private API 
    private static readonly string BaseUrl = "http://65.109.62.85:2030/api";
    /*private static readonly string BaseUrl = "http://95.217.183.87:2030/api";*/
    /*private static readonly string BaseUrl = "http://localhost:5164/api";*/
    public static string GetGroups => $"{BaseUrl}/Group";
    public static string PostGroup => $"{BaseUrl}/Group/AddGroup";
    public static string PostProxy => $"{BaseUrl}/Proxy/UploadProxies";
    public static string UpdateProxy => $"{BaseUrl}/Proxy/UpdateProxiesRC";
    public static string ReplaceProxyProxy => $"{BaseUrl}/Proxy/ReplaceProxyProxy";
    public static string DeleteGroup => $"{BaseUrl}/Group/DeleteGroup";
    public static string GetEmails => $"{BaseUrl}/Emails/GetEmails";
    public static string AddEmailsToFail => $"{BaseUrl}/Emails/FailEmails";
    public static string DeleteEmails => $"{BaseUrl}/Emails/DeleteEmails";
    public static string PostEmailsProxiesUpdate => $"{BaseUrl}/Emails/UpdateProxies";
    public static string GetAllProxies => $"{BaseUrl}/Proxy/GetProxies";
    public static string GetAddEmails => $"{BaseUrl}/Emails/AddEmails";
    public static string UpdateStatsEmails => $"{BaseUrl}/Emails/UpdateStats";
    public static string NetworkLogAdd => $"{BaseUrl}/Emails/AddNetworkLogs";
    public static string MailboxesAdd => $"{BaseUrl}/Emails/AddMailBoxes";
    public static string GetAllMailBoxes => $"{BaseUrl}/Emails/GetAllMailBoxes";
    public static string DeleteAllMailboxes => $"{BaseUrl}/Emails/DeleteAllMailboxes";
    public static string GetAllEmailsWithMailboxes => $"{BaseUrl}/Emails/GetAllEmailsWithMailboxes";
    
    
    
    
    // webShare api end points 
    private static readonly string _webShareBaseUrl = "https://proxy.webshare.io/api/v2/proxy/list";
    public static string GetReplacedProxies = $"{_webShareBaseUrl}/replaced/?page_size=100";
    
}