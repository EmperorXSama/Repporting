namespace RepportingApp.CoreSystem.FileSystem;

public static class FileManager
{

    public static string GetMasterIdsPath()
    {
        string desktopPath = GetDesktopPath();
        string masterIdsPath = Path.Combine(desktopPath,"YahooEmails","Repporting","Data","MasterIds.txt");
        return masterIdsPath;
    }

    public static string GetProfileCookieFolder(string email)
    {
        string desktopPath = GetDesktopPath();
        string cookiesPath = Path.Combine(desktopPath,"YahooEmails","Repporting","Data","Cookies",email);
        return cookiesPath;
    }
    public static string GetDesktopPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }
}