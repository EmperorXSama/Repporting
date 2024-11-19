namespace RepportingApp.Models.UI;

public partial class TaskInfoUiModel : ObservableObject
{
    
    public Process ProcessAssigned { get; set; }

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

    [ObservableProperty]
    private ICommand cancelCommand;

    
    [ObservableProperty] private string startTime;
    [ObservableProperty] private string finishedTime;
    [ObservableProperty] private TimeSpan _interval;
    
    [ObservableProperty]
    public ObservableCollection<ItemInfo> _itemSuccesMessasges  = new ObservableCollection<ItemInfo>();
    [ObservableProperty]  public ObservableCollection<ItemInfo> _itemFailedMessasges  = new ObservableCollection<ItemInfo>();

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