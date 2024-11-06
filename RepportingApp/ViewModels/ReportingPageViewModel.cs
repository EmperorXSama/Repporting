
namespace RepportingApp.ViewModels;

public partial class ReportingPageViewModel : ViewModelBase
{
    #region Multithread
    private readonly UnifiedTaskManager _taskManager;
    // Observable collections to track task and item progress
    public ObservableCollection<string> TaskMessages { get; } = new();
    public ObservableCollection<string> ErrorMessages { get; } = new();
    
    
    [ObservableProperty] private Guid _cancelUploadToken;
    [ObservableProperty] private string _remainingOpenTasks;

    private readonly  SystemConfigurationEstimator  _configEstimator;
    [ObservableProperty] private int _logicalProcessorCountDisplay;
    [ObservableProperty] private int _recommendedMaxDegreeOfParallelismDisplay;
    [ObservableProperty] private int _recommendedBatchSizeDisplay;
    #endregion
    
    
    #region UI
    [ObservableProperty] private bool _isMenuOpen = false;
    [ObservableProperty] private bool _isPopupOpen = false;
    [ObservableProperty] private bool _isNotificationOpen = false;

    [ObservableProperty] private bool _isFixed;
    [ObservableProperty] private bool _isRandom; 
    [ObservableProperty] private bool _isOneByOne;
    [ObservableProperty] private bool _isAll;
    [ObservableProperty] private ObservableCollection<string> _reportingselectedProcessesToIcon = new ObservableCollection<string>();
   
    #endregion

    #region Logic
    [ObservableProperty] private ReportingSettingValues _reportingSettingsValuesDisplay;
    [ObservableProperty]private ProxySittings _selectedProxySetting;
    [ObservableProperty]private ReportingSettings _selectedReportSetting;
    [ObservableProperty] private ReportingSittingsProcesses _selectedProcesses = new ReportingSittingsProcesses();
    #endregion

    #region Upload Files/Data

    [ObservableProperty] private bool _isUploadPopupOpen = false;
    [ObservableProperty] private string _filePath;
    [ObservableProperty] private int _uploadProgress; 
    [ObservableProperty] private string _fileName = "file name";
    [ObservableProperty] private Double _fileSize = 0;
    [ObservableProperty] private bool _isUploading; 
    private CancellationTokenSource _cancellationTokenSource; 
    [ObservableProperty] private ObservableCollection<EmailAccount> _emailAccounts = new();
    #endregion
    

 
    private readonly TaskInfoManager _taskInfoManager;
    [ObservableProperty]
    private int _tasksCount;
    public ObservableCollection<TaskInfo> ActiveTasks => _taskInfoManager.GetTasks(TaskCategory.Active);
    public ObservableCollection<TaskInfo> NotificationTasks => _taskInfoManager.GetTasks(TaskCategory.Notification);
    public ObservableCollection<TaskInfo> CampaignTasks => _taskInfoManager.GetTasks(TaskCategory.Campaign);
    public ReportingPageViewModel(IMessenger messenger,SystemConfigurationEstimator configEstimator,TaskInfoManager taskInfoManager) : base(messenger)
    {
        _taskInfoManager = taskInfoManager;
        var systemEstimator = new SystemConfigurationEstimator();
        _taskManager = new UnifiedTaskManager(systemEstimator.RecommendedMaxDegreeOfParallelism,_taskInfoManager);
        ReportingSettingsValuesDisplay = new ReportingSettingValues(App.Configuration);
        _configEstimator = configEstimator;
        taskInfoManager.PropertyChanged += TaskManager_PropertyChanged;
        AssignThreadDefaultValues();
        SubscribeEventToThread();

        Groups = DummyDataGenerator.EmailGroups.ToObservableCollection();
        IsNewGroupSelected = false;
        IsExistingGroupSelected = false;
        TasksCount = _taskInfoManager.GetTasksCount();
    }


    #region Relay commands

    [RelayCommand]
    private Task StartOperation()
    {
        var modifier = new StartProcessNotifierModel()
        {
            ReportingSettingsP = SelectedProcesses,
            Thread = ReportingSettingsValuesDisplay.Thread,
            Repetition = ReportingSettingsValuesDisplay.Repetition,
            RepetitionDelay = ReportingSettingsValuesDisplay.RepetitionDelay,
            SelectedProxySetting =  this.SelectedProxySetting,
            SelectedReportSetting =  this.SelectedReportSetting,
        };
        _messenger.Send(new ProcessStartMessage("Success","Reporting",modifier));
        // Example usage: Start a single task
        var task1 = _taskManager.StartTask(async cancellationToken =>
        {
            // Simulate some asynchronous work
            await Task.Delay(155000, cancellationToken);
            
        });
        CreateAnActiveTask(TaskCategory.Active,TakInfoType.Single, task1, "Preparation",Statics.UploadFileColor,Statics.UploadFileColor);
        // Example usage: Start a batch process
        var items = new[] { "Email1", "Email2", "Email3" }; // Sample batch items
        var task2 = _taskManager.StartBatch(items, async (item,cancellationToken) =>
        {
            // Simulate processing each item
            await Task.Delay(2500, cancellationToken);
            if (item == "Email2") throw new Exception("Simulated failure"); // Simulate error
            await Task.Delay(5500, cancellationToken);
        }, batchSize: 3);
        CreateAnActiveTask(TaskCategory.Active ,TakInfoType.Batch, task2, "Repporting",Statics.ReportingColor,Statics.ReportingSoftColor);
        return Task.CompletedTask;
    }
    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        await ReportingSettingsValuesDisplay.SaveConfigurationAsync();
    }
   [RelayCommand] 
    private void ProcessesCheckedManager()
    {
        // Define a mapping between the process selection and the corresponding icons
        var processIconMapping = new Dictionary<Func<ReportingSittingsProcesses, bool>, string>
        {
            { p => p.IsReportingSelected, "ReportingIcon" },
            { p => p.IsDeleteSpamSelected, "DeleteSpamIcon" },
            { p => p.IsGetSpamNumbersSelected, "CollectSpamNumberIcon" },
            { p => p.IsGetSpamSubjectSelected, "CollectSpamSubjectIcon" },
            { p => p.IsFixAfterFinish, "FixIcon" },
            
        };

        // Iterate through the mapping
        foreach (var mapping in processIconMapping)
        {
            var isSelected = mapping.Key(SelectedProcesses);  // Use _selectedProcesses instead of SelectedProcesses
            var iconName = mapping.Value;

            if (isSelected)
            {
                // Add the icon if it is selected
                if (!ReportingselectedProcessesToIcon.Contains(iconName))  // Corrected the collection reference
                {
                    ReportingselectedProcessesToIcon.Add(iconName);
                }
            }
            else
            {
                // Remove the icon if it is not selected
                if (ReportingselectedProcessesToIcon.Contains(iconName))  // Corrected the collection reference
                {
                    ReportingselectedProcessesToIcon.Remove(iconName);
                }
            }
        }
    }
    
    [RelayCommand]
    private void ToggleSidePanel()
    {
        IsMenuOpen = !IsMenuOpen;
    }
    [RelayCommand]
    private void SelectProxySettings(string speed)
    {
        IsFixed = speed == "Fixed";
        IsRandom = speed == "Random";
        if (IsFixed)
        {
            SelectedProxySetting = ProxySittings.Fixed;
        }else if (IsRandom)
        {
            SelectedProxySetting = ProxySittings.Random;
        }
        
    } 
    [RelayCommand]
    private void SelectReportingSettings(string speed)
    {
        IsOneByOne = speed == "OneByOne";
        IsAll = speed == "All";
        if (IsOneByOne)
        {
            SelectedReportSetting = ReportingSettings.OneByOne;
        }else if (IsAll)
        {
            SelectedReportSetting =  ReportingSettings.All;
        }
    }
    [RelayCommand] private void OpenPopup()  => IsPopupOpen = true;
    [RelayCommand] private void ClosePopup()  => IsPopupOpen = false;
    [RelayCommand] private void OpenNotificationopup()  => IsNotificationOpen = !IsNotificationOpen;
    #endregion

    #region Group Selection and creation

    [ObservableProperty] private bool _isNewGroupSelected;
    [ObservableProperty] private bool _isExistingGroupSelected; 
    [ObservableProperty] private bool _isEnabled = true; 
    public ObservableCollection<EmailGroup> Groups { get; }
    [ObservableProperty] private string _newGroupName;
    [ObservableProperty] private EmailGroup? _selectedEmailGroup = new EmailGroup(); 
    [RelayCommand]
    private void SelectNewGroup()
    {
        IsNewGroupSelected = true;
        IsExistingGroupSelected = false;
        SelectedEmailGroup = new EmailGroup();
    }

    [RelayCommand]
    private void SelectExistingGroup()
    {
        IsNewGroupSelected = false;
        IsExistingGroupSelected = true;
    }

    [RelayCommand]
    private async Task CreateNewGroup()
    {
        IsEnabled = false;
        if (!string.IsNullOrWhiteSpace(NewGroupName))
        {
            EmailGroup newGroup = new EmailGroup()
            {
                Id = Groups.Count + 1,
                Name = NewGroupName
            };

            var taskId = _taskManager.StartTask(async cancellationToken =>
            {
                // todo : code to create ( call method ) group in database
                await Task.Delay(2500, cancellationToken);
                _taskManager.UpdateUiThreadValues(
                    ()=>   Groups.Add(newGroup),
                                ()=>   IsEnabled = true,
                                ()=>    SelectedEmailGroup = newGroup);
             
            });
            
            NewGroupName = string.Empty;
           
        }
        else
        {
            IsEnabled = true;
        }
    }
    #endregion

    #region Upload File/Data Func
    
    [RelayCommand] private void OpenFileUploadPopup()  => IsUploadPopupOpen = true;

    [RelayCommand] private void CloseFileUploadPopup()
    {
        IsUploading = false;
        UploadProgress = 0;
        IsUploadPopupOpen = false;
    } 
    
    [RelayCommand]
    public async void OnDropFile(string filePath)
    {
            if (!ValidateFile(filePath))
            {
                // Optionally notify user of invalid file
                return;
            }
            var fileInfo = new FileInfo(filePath);

            // Get file name and size
            FileName = fileInfo.Name; // Get the file name
            long fileSize = fileInfo.Length;  // Get the file size in bytes

            // Optionally convert size to KB or MB for display
            FileSize =Math.Floor(fileSize / (1024.0 * 1024.0)) ; // Convert to MB
            _cancellationTokenSource = new CancellationTokenSource();
            IsUploading = true;
            UploadProgress = 0;
            var currentTaskId  = _taskManager.StartTask(async cancellationToken =>
            {
                try
                {
                    var progress = new Progress<int>(value => { UploadProgress = value; });
                    await UploadFileContentAsync(filePath, progress, cancellationToken);
                }
                finally
                {
                    Dispatcher.UIThread.Post(() =>IsUploading = false );
                    UploadProgress = 0;
                }
                
            });
            CancelUploadToken = currentTaskId;
            CreateAnActiveTask(TaskCategory.Active,TakInfoType.Single, currentTaskId, "File Upload",Statics.UploadFileColor,Statics.UploadFileSoftColor);
        
    }

    private async Task UploadFileContentAsync(string filePath, IProgress<int> progress, CancellationToken cancellationToken)
    {
        /*
         * todo : see if we need to create a new group or use existing group
         * todo : upload on chunks instead of one by one
         * todo : add each chunk to database
         * todo : then update ui of the progress
         */
        // Read all lines from the file
        var lines = await File.ReadAllLinesAsync(filePath);
        var totalLines = lines.Length;

        for (int i = 0; i < totalLines; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = lines[i];
            var data = line.Split(';');

            if (data.Length != 7)
            {
                // Handle incorrect data format, e.g., skip the line or log an error
                continue;
            }

            // Simulate processing of the line (replace with actual upload logic)
            await Task.Delay(3000); // Simulate time-consuming operation

            // Create and add a new EmailAccount with parsed data
            var emailAccount = new EmailAccount
            {
                EmailAddress = data[0],
                Password = data[1],
                RecoveryEmail = data[2],
                Proxy = new Proxy
                {
                    ProxyIp = data[3],
                    Port = int.TryParse(data[4], out var port) ? port : 0,
                    Username = data[5],
                    Password = data[6]
                },
                Status = EmailStatus.NewAdded,
                GroupId = 1 // Replace with actual logic if needed
            };

            // Add the account to the observable collection on the UI thread
            Dispatcher.UIThread.Post(() => EmailAccounts.Add(emailAccount));

            // Report progress
            int percentComplete = (int)((i + 1) * 100.0 / totalLines);
            progress.Report(percentComplete);
        }
    }
    private bool ValidateFile(string filePath)
    {
        var validExtensions = new[] { ".xlsx", ".xls", ".txt" };
        var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
        return validExtensions.Contains(fileExtension);
    }
    [RelayCommand]
    public void CancelTask(Guid taskId)
    {
        _taskManager.CancelTask(taskId);
    }

    [RelayCommand]
    private void ClearAllNotifications()
    {
        _taskInfoManager.ClearTasks(TaskCategory.Notification);
    }
    #endregion


    #region Thread Logic

    private void AssignThreadDefaultValues()
    {
        
        LogicalProcessorCountDisplay = _configEstimator.LogicalProcessorCount;
        RecommendedMaxDegreeOfParallelismDisplay = _configEstimator.RecommendedMaxDegreeOfParallelism;
        RecommendedBatchSizeDisplay = _configEstimator.RecommendedBatchSize;
        RemainingOpenTasks = $"{RecommendedMaxDegreeOfParallelismDisplay - _taskInfoManager.GetTasks(TaskCategory.Active).Count} Open";
    }
    
    public void CreateAnActiveTask(TaskCategory category, TakInfoType type, Guid taskId, string name, string color, string softColor)
    {
        var taskInfo = new TaskInfo(type)
        {
            TaskId = taskId,
            Name = name,
            Color = color,
            SoftColor = softColor,
            CancelCommand = new RelayCommand(() =>
            {
                CancelTask(taskId);
                _taskInfoManager.CompleteTask(taskId,category);
                TasksCount = _taskInfoManager.GetTasksCount();
            })
        };

        _taskInfoManager.AddTask(category, taskInfo);
        TasksCount = _taskInfoManager.GetTasksCount();
    }
    
    
    private void TaskManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Notify the UI if the collection property itself is updated
        if (e.PropertyName == nameof(_taskInfoManager.ActiveTasks))
        {
            OnPropertyChanged(nameof(ActiveTasks));
            
        }
        else if (e.PropertyName == nameof(_taskInfoManager.NotificationTasks))
        {
            OnPropertyChanged(nameof(NotificationTasks));
        }
        else if (e.PropertyName == nameof(_taskInfoManager.CampaignTasks))
        {
            OnPropertyChanged(nameof(CampaignTasks));
        }
        
        
        
    }
    #endregion
    #region events and subscription

    private void SubscribeEventToThread()
    {
        _taskManager.TaskCompleted += OnTaskCompleted;
        _taskManager.TaskErrored += OnTaskErrored;
        _taskManager.BatchCompleted += OnBatchCompleted;
        _taskManager.ItemProcessed += OnItemProcessed;
        
        _taskInfoManager.GetTasks(TaskCategory.Active).CollectionChanged += OnActiveTasksChanged;
        _taskInfoManager.GetTasks(TaskCategory.Campaign).CollectionChanged += OnCampaignTasksChanged;
    }
    private void OnActiveTasksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        
        RemainingOpenTasks = $"{RecommendedMaxDegreeOfParallelismDisplay -  _taskInfoManager.GetTasksCount()} Open";
    } 
    private void OnCampaignTasksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        
        RemainingOpenTasks = $"{RecommendedMaxDegreeOfParallelismDisplay -  _taskInfoManager.GetTasksCount()} Open";
    } 
    private void OnTaskCompleted(object? sender, TaskCompletedEventArgs e)
    {
        TaskMessages.Add($"Task {e.TaskId} completed successfully.");
        _taskInfoManager.CompleteTask(e.TaskId);
    }
    
    private void OnTaskErrored(object? sender, TaskErrorEventArgs e)
    {
        ErrorMessages.Add($"Task {e.TaskId} encountered an error: {e.Error.Message}");
        _taskInfoManager.CompleteTask(e.TaskId);
    }

    private void OnBatchCompleted(object? sender, BatchCompletedEventArgs e)
    {
        TaskMessages.Add($"Batch task {e.TaskId} completed.");
        _taskInfoManager.CompleteTask(e.TaskId);
      
    }

    private void OnItemProcessed(object? sender, ItemProcessedEventArgs e)
    {
        if (e.Success)
        {
            var taskInfo =  _taskInfoManager.GetTasks(TaskCategory.Active).FirstOrDefault(t => t.TaskId == e.TaskId);
            taskInfo.ItemSuccesMessasges.Add(new ItemInfo(){Message = $"Item {e.Item} processed successfully in task {e.TaskId}."});
            TaskMessages.Add($"Item {e.Item} processed successfully in task {e.TaskId}.");
        }
        else
        {
            var taskInfo =  _taskInfoManager.GetTasks(TaskCategory.Active).FirstOrDefault(t => t.TaskId == e.TaskId);
            taskInfo.ItemFailedMessasges.Add(new ItemInfo(){Message = $"Item {e.Item} failed in task {e.TaskId}: {e.Error?.Message}"});
            ErrorMessages.Add($"Item {e.Item} failed in task {e.TaskId}: {e.Error?.Message}");
        }
    }

    #endregion

    #region campaign

    [RelayCommand]
    public Task AddSingleCampaignTask()
    {
        TimeSpan interval = TimeSpan.FromSeconds(10);
        Func<CancellationToken, Task> taskFunc = async token =>
        {
            // Task logic goes here
            await Task.Delay(6000, token); // Simulate some work
        };
        Guid taskId = _taskManager.StartLoopingTask(taskFunc, interval);
        CreateAnActiveTask(TaskCategory.Campaign,TakInfoType.Single,taskId,"reporting",Statics.UploadFileColor,Statics.UploadFileSoftColor);
        return Task.CompletedTask;
    }

    #endregion
   
}
public partial class TaskInfo : ObservableObject
{
    [ObservableProperty]
    private TimeSpan _timeUntilNextRun;
    
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
    public TaskInfo(TakInfoType takInfoType)
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