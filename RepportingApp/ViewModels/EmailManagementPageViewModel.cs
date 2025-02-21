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
            ErrorMessage = $"";
            SuccessMessage = $"";
            ErrorMessage = String.Empty;
            await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.DeleteEmails,EmailInput);
            EmailInput = string.Empty;
        }
        catch (Exception e)
        {
          ErrorMessage = $"Error: {e.Message}";
        }
      
    }
    [RelayCommand]
    private async Task DownloadEmailsAsync()
    {
        try
        {
            ErrorMessage = $"";
            SuccessMessage = $"";
            if (string.IsNullOrWhiteSpace(SelectedEmailGroupFilter.GroupName)) // Ensure group name is selected
            {
                ErrorMessage = "Please select a group.";
                return;
            }

           
            var emailsInGroup = _networkItems
                .Where(email => email.Group?.GroupName == SelectedEmailGroupFilter.GroupName)
                .ToList();

            if (emailsInGroup.Count == 0)
            {
                ErrorMessage = "No emails found in the selected group.";
                return;
            }

            // Prepare file content
            var sb = new StringBuilder();
            foreach (var email in emailsInGroup)
            {
                sb.AppendLine($"{email.EmailAddress};{email.Password};;{email.Proxy.ProxyIp};{email.Proxy.Port};{email.Proxy.Username};{email.Proxy.Password}");
            }

            // Define file path
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string timestamp = DateTime.Now.ToString("HH-mm");
            string fileName = $"{SelectedGroupName}_{timestamp}.txt";
            string filePath = Path.Combine(desktopPath, fileName);

            // Write to file
            await File.WriteAllTextAsync(filePath, sb.ToString());

            SuccessMessage = $"File saved to: {filePath}";
        }
        catch (Exception e)
        {
            ErrorMessage = $"Error: {e.Message}";
        }
    }

    [ObservableProperty] private bool _isLoading;
    public async Task LoadDataIfFirstVisitAsync(bool ignorecache = false)
    {
        try
        {
            if (!IsLoading)
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
    [ObservableProperty] private EmailGroup? _selectedEmailGroupFilter = null;
    private async Task LoadDataAsync(bool ignoreCache)
    {
        var fetchEmailsTask = _apiConnector.GetDataAsync<IEnumerable<EmailAccount>>(ApiEndPoints.GetEmails, ignoreCache:ignoreCache);
        var fetchGroupsTask = _apiConnector.GetDataAsync<IEnumerable<EmailGroup>>(ApiEndPoints.GetGroups, ignoreCache:ignoreCache);

        // Await tasks separately
        var groups = await fetchGroupsTask;
        var emails = await fetchEmailsTask;
        Groups = groups.ToObservableCollection();
        NetworkItems = emails.ToObservableCollection();

    }
}