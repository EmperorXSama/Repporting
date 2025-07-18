using System.Net;
using System.Text;

namespace RepportingApp.ViewModels;

public partial class EmailManagementPageViewModel: ViewModelBase , ILoadableViewModel
{
    private readonly IApiConnector _apiConnector;
    [ObservableProperty]
    private string _emailInput;   
    [ObservableProperty]
    private string _selectedGroupName; 
    [ObservableProperty]
    private string _errorMessage;  
    [ObservableProperty]
    private string _successMessage;
    [ObservableProperty] private ObservableCollection<EmailAccount> _networkItems;
    public EmailManagementPageViewModel(IMessenger messenger,IApiConnector apiConnector) : base(messenger)
    {
        _apiConnector = apiConnector;
    }
    
  [RelayCommand]
private async Task DeleteEmailsAsync()
{
    try
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        var response = await _apiConnector.PostDataObjectAsync<dynamic>(ApiEndPoints.DeleteEmails, EmailInput);
        
        if (response != null && response.deletedEmails  != null)
        {
            int deletedCount = response.deletedEmails;
            SuccessMessage = $"{deletedCount} emails have been deleted successfully.";
        }
        else
        {
            SuccessMessage = "No emails were deleted.";
        }

        EmailInput = string.Empty;
    }
    catch (Exception e)
    {
        ErrorMessage = $"Error: {e.Message}";
    }
}

[ObservableProperty] private bool _downloadAllGroups = false;
[ObservableProperty] private bool _overwriteDownloadFile = true;

[RelayCommand]
private async Task DownloadEmailsAsync()
{
    try
    {
        ErrorMessage = "";
        SuccessMessage = "";

        if ((!SelectedEmailGroupFilters.Any() || string.IsNullOrWhiteSpace(SelectedEmailGroupFilters[0].GroupName)))
        {
            ErrorMessage = "Please select a group .";
            return;
        }

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string outputFolder = Path.Combine(desktopPath, "YahooEmails", "Repporting", "Data");
        Directory.CreateDirectory(outputFolder); // Ensure directory exists

        StringBuilder sb = new();
        StringBuilder networkLog = new();
        var selectedGroupNames = SelectedEmailGroupFilters.Select(group => group.GroupName).ToHashSet();

        var emailsInGroup = NetworkItems
            .Where(email => selectedGroupNames.Contains(email.Group.GroupName))
            .ToList();


        if (!emailsInGroup.Any())
        {
            ErrorMessage = "No emails found in the selected group.";
            return;
        }
        
        foreach (var email in emailsInGroup)
        {
            sb.AppendLine($"{email.EmailAddress};{email.Password};;{email.Proxy?.ProxyIp};{email.Proxy?.Port};{email.Proxy?.Username};{email.Proxy?.Password}");
            if (email.MetaIds != null)
            {
                networkLog.AppendLine(
                    $"{email.EmailAddress};{email.MetaIds.MailId};{email.MetaIds.YmreqId};{email.MetaIds.Wssid}");
                /*string cookieFile = Path.Combine(desktopPath, "YahooEmails", "Repporting", "Data","Cookies",$"{email.EmailAddress}.txt");
                await File.WriteAllTextAsync(cookieFile, email.MetaIds?.Cookie);*/
            }
          
        }
        string filePath;
        string masterIdsPath = Path.Combine(outputFolder, "MasterIds.txt");

        if (OverwriteDownloadFile)
        {
            filePath = Path.Combine(outputFolder, "Profiles.txt");
            // Write to file
            await File.WriteAllTextAsync(filePath, sb.ToString());
            await File.WriteAllTextAsync(masterIdsPath, networkLog.ToString());
        }
        else if (DownloadAllGroups)
        {
            filePath = Path.Combine(outputFolder, "SeparatedProfiles.txt");
            // Write to file
            await File.WriteAllTextAsync(filePath, sb.ToString());
            await File.WriteAllTextAsync(masterIdsPath, networkLog.ToString());
        }
        else
        {
            filePath = Path.Combine(outputFolder, "Profiles.txt");
            await File.AppendAllTextAsync(filePath, sb.ToString());
            await File.AppendAllTextAsync(masterIdsPath, networkLog.ToString());
        }
        
        SuccessMessage = $"File saved to: {filePath}";
    }
    catch (Exception e)
    {
        ErrorMessage = $"Error: {e.Message}";
    }
}

[ObservableProperty] private int _availableProxies = 0;
[ObservableProperty] private int _usedProxies = 0;
    [ObservableProperty] private bool _isLoading;
    public bool IsLoadNext { get; set; } = false;

    public async Task LoadDataIfFirstVisitAsync(bool ignorecache = false)
    {
        try
        {
            if (!IsLoading )
            {
                IsLoading = true;
                // Load data here
                await LoadDataAsync(ignorecache);
            }
        }
        catch (Exception e)
        {
            ErrorMessage = $"Error: {e.Message}";
        }
        finally
        {
            IsLoading = false;
        }

    }
    [ObservableProperty] private ObservableCollection<EmailGroup> _groups= new ObservableCollection<EmailGroup>();
    [ObservableProperty] public ObservableCollection<Proxy> centralProxyList = new ObservableCollection<Proxy>();
    [ObservableProperty]
    private ObservableCollection<EmailGroup> _selectedEmailGroupFilters = new();

    private async Task LoadDataAsync(bool ignoreCache)
    {
        var fetchEmailsTask = _apiConnector.GetDataAsync<IEnumerable<EmailAccount>>(ApiEndPoints.GetEmails, ignoreCache:ignoreCache);
        var fetchGroupsTask = _apiConnector.GetDataAsync<IEnumerable<EmailGroup>>(ApiEndPoints.GetGroups, ignoreCache:ignoreCache);
        var proxies = await _apiConnector.GetDataAsync<IEnumerable<Proxy>>(ApiEndPoints.GetAllProxies,ignoreCache:false);
        var enumerable = proxies as Proxy[] ?? proxies.ToArray();
        CentralProxyList = enumerable.ToObservableCollection();
        // Await tasks separately
        var groups = await fetchGroupsTask;
        var emails = await fetchEmailsTask;
        Groups = groups.ToObservableCollection();
        NetworkItems = emails.ToObservableCollection();
        AvailableProxies = enumerable.Count(p => p.Availability);
        UsedProxies = enumerable.Count(p => p.Availability == false);
    }
    
    
    [ObservableProperty] private string _emailInputAssign;

[RelayCommand]
private async Task AssignProxiesAndSaveToFileAsync()
{
    try
    {
        bool isOverwriting = false;
        if (string.IsNullOrWhiteSpace(EmailInputAssign))
        {
            isOverwriting = true;
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "YahooEmails", "Repporting", "Data", "Profiles.txt");

            if (!File.Exists(filePath))
            {
                ErrorMessage = "Profiles.txt not found!";
                return;
            }

            EmailInputAssign = await File.ReadAllTextAsync(filePath);
        }

        // Parse input into email accounts
        var emailAccounts = EmailInputAssign.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(';'))
            .Where(parts => parts.Length >= 2)
            .Select(parts => new EmailAccount
            {
                EmailAddress = parts[0].Trim(),
                Password = parts[1].Trim()
            })
            .ToList();

        if (emailAccounts.Count == 0)
        {
            ErrorMessage = "No valid emails found!";
            return;
        }

        // Get available proxies first, then all proxies
        var availableProxies = CentralProxyList.Where(p => p.Availability).ToList();
        var allProxies = CentralProxyList.ToList();

        // Step 1: Try to assign available proxies first
        await AssignProxiesWithTesting(emailAccounts, availableProxies);

        // Step 2: If emails still need proxies, use all proxies with subnet arrangement
        var emailsWithoutProxies = emailAccounts.Where(e => e.Proxy == null).ToList();
        if (emailsWithoutProxies.Any())
        {
            // Get unused proxies and arrange by subnet
            var usedProxies = emailAccounts.Where(e => e.Proxy != null).Select(e => e.Proxy).ToList();
            var unusedProxies = allProxies.Except(usedProxies).ToList();
            var shuffledProxies = RearrangeProxiesBySubnet(unusedProxies);

            await AssignProxiesWithTesting(emailsWithoutProxies, shuffledProxies);
        }

        // Step 3: If still emails without proxies, reuse working proxies
        var finalEmailsWithoutProxies = emailAccounts.Where(e => e.Proxy == null).ToList();
        if (finalEmailsWithoutProxies.Any())
        {
            var workingProxies = emailAccounts.Where(e => e.Proxy != null).Select(e => e.Proxy).ToList();
            
            if (workingProxies.Any())
            {
                for (int i = 0; i < finalEmailsWithoutProxies.Count; i++)
                {
                    finalEmailsWithoutProxies[i].Proxy = workingProxies[i % workingProxies.Count];
                }
            }
            else
            {
                ErrorMessage = "No working proxies found for email assignment!";
                return;
            }
        }

        if (isOverwriting)
        {
            // Save the processed email list to "assignedFinish.txt"
            await SaveEmailsToFileAsync(emailAccounts, isOverwriting);
            SuccessMessage = "Proxies assigned and file saved successfully!";
        }
        
        EmailInputAssign = string.Join(Environment.NewLine, emailAccounts.Select(e => 
            $"{e.EmailAddress};{e.Password};;{e.Proxy.ProxyIp};{e.Proxy.Port};{e.Proxy.Username};{e.Proxy.Password}"));
    }
    catch (Exception ex)
    {
        ErrorMessage = $"Error: {ex.Message}";
    }
}
// Method to assign proxies with on-demand testing
private async Task AssignProxiesWithTesting(List<EmailAccount> emailAccounts, List<Proxy> proxyPool)
{
    int emailIndex = 0;
    int proxyIndex = 0;

    while (emailIndex < emailAccounts.Count && proxyIndex < proxyPool.Count)
    {
        var currentProxy = proxyPool[proxyIndex];
        
        // Test the proxy only when we're about to assign it
        bool isProxyWorking = await TestProxyAsync(currentProxy);
        
        if (isProxyWorking)
        {
            // Proxy works, assign it to the email
            emailAccounts[emailIndex].Proxy = currentProxy;
            emailIndex++; // Move to next email
        }
        // If proxy failed, just move to next proxy (don't increment emailIndex)
        
        proxyIndex++; // Always move to next proxy
    }
}

// Fast proxy testing method
private async Task<bool> TestProxyAsync(Proxy proxy)
{
    try
    {
        // Skip testing if we already know it's working
        if (proxy.YahooConnectivity == "Working")
        {
            return true;
        }

        using (var httpClient = new HttpClient())
        {
            var proxyUri = new Uri($"http://{proxy.ProxyIp}:{proxy.Port}");
            var webProxy = new WebProxy(proxyUri);
            
            if (!string.IsNullOrEmpty(proxy.Username) && !string.IsNullOrEmpty(proxy.Password))
            {
                webProxy.Credentials = new NetworkCredential(proxy.Username, proxy.Password);
            }

            var httpClientHandler = new HttpClientHandler()
            {
                Proxy = webProxy,
                UseProxy = true
            };

            using (var proxyClient = new HttpClient(httpClientHandler))
            {
                proxyClient.Timeout = TimeSpan.FromSeconds(5); // 5 second timeout
                
                var response = await proxyClient.GetAsync("http://httpbin.org/ip");
                
                // Update proxy status
                proxy.YahooConnectivity = response.IsSuccessStatusCode ? "Working" : "Failed";
                
                return response.IsSuccessStatusCode;
            }
        }
    }
    catch (Exception ex)
    {
        proxy.YahooConnectivity = $"Failed: {ex.Message}";
        return false;
    }
}
    /// <summary>
    /// Saves the list of emails with assigned proxies to "assignedFinish.txt".
    /// </summary>
    private async Task SaveEmailsToFileAsync(List<EmailAccount> emailAccounts,bool isOverwriting)
    {
        try
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = isOverwriting  ? Path.Combine(desktopPath, "YahooEmails", "Repporting", "Data", "Profiles.txt") 
                : Path.Combine(desktopPath, "YahooEmails", "Repporting", "Data", "assignedFinish.txt") ;
            
           

            var sb = new StringBuilder();
            foreach (var email in emailAccounts)
            {
                string proxyData = email.Proxy != null
                    ? $"{email.Proxy.ProxyIp};{email.Proxy.Port};{email.Proxy.Username};{email.Proxy.Password}"
                    : "NoProxy";

                sb.AppendLine($"{email.EmailAddress};{email.Password};;{proxyData}");
            }

            await File.WriteAllTextAsync(filePath, sb.ToString());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving file: {ex.Message}";
        }
    }

    /// <summary>
    /// Rearranges proxies to distribute proxies with the same subnet as evenly as possible.
    /// </summary>
    private List<Proxy> RearrangeProxiesBySubnet(List<Proxy> proxies)
    {
        var groupedBySubnet = proxies
            .GroupBy(proxy => proxy.ProxyIp.Substring(0, proxy.ProxyIp.LastIndexOf('.')))
            .OrderBy(g => g.Count()) // Sort by subnet size (smallest first)
            .Select(g => g.ToList()) // Convert groupings to lists for modification
            .ToList();

        var result = new List<Proxy>();
        while (groupedBySubnet.Any(g => g.Any()))
        {
            foreach (var group in groupedBySubnet.Where(g => g.Any()).ToList()) // Only iterate over non-empty groups
            {
                result.Add(group.First());
                group.RemoveAt(0); // Remove assigned proxy from the group
            }
        }

        return result;
    }



}