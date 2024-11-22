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
    private string softColor = ""; 
    [ObservableProperty] private bool _isMoreDetailNeeded = false;
    [ObservableProperty] private bool _isConsoleNeeded = false;

    [ObservableProperty]
    private ICommand cancelCommand;

    
    [ObservableProperty] private string startTime;
    [ObservableProperty] private string finishedTime;
    [ObservableProperty] private TimeSpan _interval;
    
    [ObservableProperty]
    public ObservableCollection<ItemInfo> _itemSuccesMessasges  = new ObservableCollection<ItemInfo>();
    [ObservableProperty]  public ObservableCollection<ItemInfo> _itemFailedMessasges  = new ObservableCollection<ItemInfo>();
    [ObservableProperty]  public ObservableCollection<EmailAccount>? _assignedGroup  = new ObservableCollection<EmailAccount>();
    [ObservableProperty] public EmailAccount _selectedEmail;
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
    public TaskInfoUiModel(TakInfoType takInfoType)
    {
        TakInfoType = takInfoType;
        startTime = DateTime.UtcNow.ToString("t");
    }
    #region RC

    
    [RelayCommand]
    private void ToggleViewMoreDetails()
    {
        IsMoreDetailNeeded = !IsMoreDetailNeeded;
    }
    [RelayCommand]
    private void ToggleViewConsoleDetails()
    {
        IsConsoleNeeded = !IsConsoleNeeded;
    } 
    [RelayCommand]
    private void CloseConsoleDetailNeede()
    {
        IsConsoleNeeded = false;
    }

    #endregion
}


public class ItemInfo()
{
    public EmailAccount Email { get; set; }
    public string Message { get; set; }
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