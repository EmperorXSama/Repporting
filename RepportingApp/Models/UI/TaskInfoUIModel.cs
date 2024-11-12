namespace RepportingApp.Models.UI;

public partial class TaskInfoUiModel : ObservableObject
{
    
    public Process ProcessAssigned { get; set; }

    [ObservableProperty]
    private TimeSpan _timeUntilNextRun;
    [ObservableProperty]
    private TaskStatus _workingStatus = TaskStatus.Waiting;
    [ObservableProperty]
    private Guid taskId;

    [ObservableProperty]
    private string name; 
    [ObservableProperty]
    private string color = ""; 
    [ObservableProperty]
    private string softColor = "";

    [ObservableProperty]
    private ICommand cancelCommand;
    
    [ObservableProperty] private string startTime;
    [ObservableProperty] private string finishedTime;
    
    public ObservableCollection<ItemInfo> ItemSuccesMessasges { get; set; } = new ObservableCollection<ItemInfo>();
    public ObservableCollection<ItemInfo> ItemFailedMessasges { get; set; } = new ObservableCollection<ItemInfo>();

    [ObservableProperty]
    private string taskMessage;
    
    public TakInfoType TakInfoType { get; set; }
    public TaskInfoUiModel(TakInfoType takInfoType)
    {
        TakInfoType = takInfoType;
        startTime = DateTime.Now.ToString("t");
    }
}


public class ItemInfo()
{
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
}