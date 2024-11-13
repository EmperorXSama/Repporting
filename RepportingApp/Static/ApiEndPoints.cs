namespace RepportingApp.CoreSystem.ApiSystem;

public class ApiEndPoints
{
    // private API 
    private static readonly string BaseUrl = "http://localhost:5164/api";
    public static string GetGroups => $"{BaseUrl}/Group";
    public static string GetEmails => $"{BaseUrl}/Emails/GetEmails";
    public static string GetAddEmails => $"{BaseUrl}/Emails/AddEmails";

    #region other api's endpoints 

    

    #endregion
}