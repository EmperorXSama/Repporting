using System.Text;

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
    public static async Task WriteEmailAccountsToFileAsync(string fileName, IEnumerable<EmailAccount> emailAccounts)
{


    // Define the desktop path and file name
    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    string filePath = Path.Combine(desktopPath, fileName);

    // Use StringBuilder for efficient string manipulation
    var sb = new StringBuilder();

    // Iterate through the email accounts and build the content
    foreach (var email in emailAccounts)
    {
        sb.AppendLine($"Email Address: {email.EmailAddress}");
        sb.AppendLine($"Metadata:");
        sb.AppendLine($"  Wssid: {email.MetaIds.Wssid}");
        sb.AppendLine($"  Cookie: {email.MetaIds.Cookie}");
        sb.AppendLine(new string('-', 50)); // Separator for better readability
    }

    // Write the content to the file
    await File.WriteAllTextAsync(filePath, sb.ToString());

    Console.WriteLine($"Email accounts information written to: {filePath}");
}

}