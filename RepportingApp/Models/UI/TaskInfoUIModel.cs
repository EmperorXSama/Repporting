using System.Diagnostics;

namespace RepportingApp.Models.UI;

public partial class TaskInfoUiModel : ObservableObject
{
    
    
    [ObservableProperty]
    private TimeSpan _timeUntilNextRun;
    [ObservableProperty]
    private TaskStatus _workingStatus = TaskStatus.Running;
    [ObservableProperty]
    private Guid taskId;
    [ObservableProperty]
    private string name; 
    [ObservableProperty]
    private string color = "";  
    [ObservableProperty]
    private string _assignedGroup = "";  
    [ObservableProperty]
    private bool isSelected;
    [ObservableProperty]
    private string downloadValue = ""; 
    [ObservableProperty]
    private string softColor = ""; 
    [ObservableProperty] private bool _isMoreDetailNeeded = false;
    [ObservableProperty] private bool _isErrorTypeNeeded = false;
    [ObservableProperty] private bool _isConsoleNeeded = false;


    [ObservableProperty]
    private ICommand cancelCommand;

    
    [ObservableProperty] private string startTime;
    [ObservableProperty] private string finishedTime;
    [ObservableProperty] private TimeSpan _interval;
    [ObservableProperty] private ObservableCollection<KeyValuePair<string, int>> _uniqueErrorMessages = new();
    [ObservableProperty]
    public ObservableCollection<ItemInfo> _itemSuccesMessasges  = new ObservableCollection<ItemInfo>();
    [ObservableProperty]  public ObservableCollection<ItemInfo> _itemFailedMessasges  = new ObservableCollection<ItemInfo>();
    [ObservableProperty]  public ObservableCollection<EmailAccount>? _assignedEmails  = new ObservableCollection<EmailAccount>();
    [ObservableProperty]  public ObservableCollection<EmailAccount>? _assignedEmailsDisplayInfo  = new ObservableCollection<EmailAccount>();
    [ObservableProperty] public EmailAccount _selectedEmail;
    [ObservableProperty] public ItemInfo _viewDetailSelectedMessage;
    partial void OnSelectedEmailChanged(EmailAccount value)
    {
        if (value != null)
        {
            // Your custom logic here
            Debug.WriteLine($"Selected email changed to: {value.EmailAddress}");
        
            // Example: Load additional details or log responses
            foreach (var response in value.ApiResponses)
            {
                Debug.WriteLine($"API Response - Key: {response.Key}, Value: {response.Value}");
            }
        }
        else
        {
            Debug.WriteLine("Selected email was deselected.");
        }
    }

    [ObservableProperty]
    private string taskMessage;
    
    public TakInfoType TakInfoType { get; set; }
    public TaskCategory StarterCategory { get; set; }
    public TaskCategory MovetoCategory { get; set; }
    public TaskInfoUiModel(TakInfoType takInfoType,TaskCategory starterCategory,TaskCategory movetoCategory)
    {
        TakInfoType = takInfoType;
        StarterCategory = starterCategory;
        MovetoCategory = movetoCategory;
    }
    #region RC

    
    [RelayCommand]
    private void ToggleViewMoreDetails()
    {
        IsMoreDetailNeeded = !IsMoreDetailNeeded;
    } 
    [RelayCommand]
    private void ToggleErrorType()
    {
        IsErrorTypeNeeded = !IsErrorTypeNeeded;
    }
    [RelayCommand]
    private void ToggleViewConsoleDetails()
    {
        
        if ( !AssignedEmailsDisplayInfo!.Any() || AssignedEmailsDisplayInfo!.FirstOrDefault()!.ApiResponses.Count <1)
        {
            return;
        }
        IsConsoleNeeded = !IsConsoleNeeded;
    } 
    [RelayCommand]
    private void CloseConsoleDetailNeede()
    {
        IsConsoleNeeded = false;
    }
    [RelayCommand]
    private void DownloadFailedEmails()
    {
        try
        {
            if (ItemFailedMessasges == null || !ItemFailedMessasges.Any())
            {
                DownloadValue = "No failed messages to download.";
                return;
            }

            // Create a list of email credentials
            var failedEmails = ItemFailedMessasges
                .Where(info => info.Email != null) // Ensure Email is not null
                .Select(info =>
                {
                    var proxy = info.Email.Proxy;
                    var proxyDetails = proxy != null
                        ? $"{proxy.ProxyIp};{proxy.Port};{proxy.Username};{proxy.Password}"
                        : "No Proxy";
                    return $"{info.Email.Id};{info.Email.EmailAddress};{info.Email.Password};;{proxyDetails};{info.Email.Group.GroupName};{info.Message}";
                })
                .ToList();

            if (!failedEmails.Any())
            {
                DownloadValue = "No valid emails to download.";
                return;
            }

            // Convert to a single string
            string content = string.Join(Environment.NewLine, failedEmails);

            // Ensure the directory exists before saving the file
            string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Results");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string filePath = Path.Combine(directoryPath, "FailedEmails.txt");
            File.WriteAllText(filePath, content);

            DownloadValue = "Download complete: " + filePath;
        }
        catch (Exception e)
        {
            DownloadValue = "Error: " + e.Message;
        }
    }

    #endregion
    
    public  string? CheckCommonGroupName(ObservableCollection<EmailAccount>? assignedEmailsDisplayInfo)
    {
        if (assignedEmailsDisplayInfo == null || assignedEmailsDisplayInfo.Count == 0)
            return null;

        // Extract all group names from the collection
        var groupNames = assignedEmailsDisplayInfo
            .Where(email => email.Group != null)
            .Select(email => email.Group.GroupName)
            .Distinct()
            .ToList();

        // If there's only one unique group name, return it
        if (groupNames.Count == 1)
            return groupNames[0];

        // If multiple group names exist, return "Custom"
        if (groupNames.Count > 1)
            return "Custom";

        // If no groups are defined, return null
        return null;
    }
}


public class ItemInfo()
{
    public EmailAccount Email { get; set; }
    public string Message { get; set; }
    public string? Title { get; set; }
}

public enum TakInfoType{
    Single,
    Batch
}

public enum TaskStatus
{
    Pending,
    Running,
    Waiting,
    Finished
}