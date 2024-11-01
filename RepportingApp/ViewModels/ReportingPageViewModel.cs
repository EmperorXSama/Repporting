

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using Reporting.lib.enums.Core;
using Reporting.lib.Models.Core;

namespace RepportingApp.ViewModels;

public partial class ReportingPageViewModel : ViewModelBase
{
    #region Multithread
    private readonly UnifiedTaskManager _taskManager;
    // Observable collections to track task and item progress
    public ObservableCollection<string> TaskMessages { get; } = new();
    public ObservableCollection<string> ErrorMessages { get; } = new();
    
    #endregion
    private readonly  SystemConfigurationEstimator  _configEstimator;
    [ObservableProperty] private int _logicalProcessorCountDisplay;
    [ObservableProperty] private int _recommendedMaxDegreeOfParallelismDisplay;
    [ObservableProperty] private int _recommendedBatchSizeDisplay;
   
    
    
    
     public ObservableCollection<TaskInfo> ActiveTasks { get; } = new();
     [ObservableProperty] private Guid _cancelUploadToken;
    
    #region UI
    [ObservableProperty] private bool _isMenuOpen = false;
    [ObservableProperty] private bool _isPopupOpen = false;

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
    [ObservableProperty]
    private string _filePath;
    #endregion
    

 
  
    public ReportingPageViewModel(IMessenger messenger,SystemConfigurationEstimator configEstimator) : base(messenger)
    {
        var systemEstimator = new SystemConfigurationEstimator();
        _taskManager = new UnifiedTaskManager(systemEstimator.RecommendedMaxDegreeOfParallelism);
     
        ReportingSettingsValuesDisplay = new ReportingSettingValues(App.Configuration);
        _configEstimator = configEstimator;
        LogicalProcessorCountDisplay = _configEstimator.LogicalProcessorCount;
        RecommendedMaxDegreeOfParallelismDisplay = _configEstimator.RecommendedMaxDegreeOfParallelism;
        RecommendedBatchSizeDisplay = _configEstimator.RecommendedBatchSize;
        
        _taskManager.TaskCompleted += OnTaskCompleted;
        _taskManager.TaskErrored += OnTaskErrored;
        _taskManager.BatchCompleted += OnBatchCompleted;
        _taskManager.ItemProcessed += OnItemProcessed;
    }


    #region Relay commands

    [RelayCommand]
    private async Task StartOperation()
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
    #endregion


    #region Upload File/Data Func
    [ObservableProperty] private int _uploadProgress; 
    [ObservableProperty] private string _fileName = "file name";
    [ObservableProperty] private Double _fileSize = 0;
    [ObservableProperty] private bool _isUploading; 
    private CancellationTokenSource _cancellationTokenSource; 
    [ObservableProperty] private ObservableCollection<EmailAccount> _emailAccounts = new();
    [RelayCommand] private void OpenFileUploadPopup()  => IsUploadPopupOpen = true;

    [RelayCommand]
    private void CloseFileUploadPopup()
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
            ActiveTasks.Add(new TaskInfo
            {
                TaskId = currentTaskId,
                Name = "File Upload",
                CancelCommand = new RelayCommand(() => CancelTask(currentTaskId))
            });
        
      
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
    #endregion
    
   
    [RelayCommand]
    private void StartTasks()
    {
        // Example usage: Start a single task
        _taskManager.StartTask(async cancellationToken =>
        {
            // Simulate some asynchronous work
            await Task.Delay(15000);
        });

        // Example usage: Start a batch process
        var items = new[] { "Email1", "Email2", "Email3" }; // Sample batch items
        _taskManager.StartBatch(items, async (item,cancellationToken) =>
        {
            // Simulate processing each item
            await Task.Delay(2500);
            if (item == "Email2") throw new Exception("Simulated failure"); // Simulate error
        }, batchSize: 3);
    }
    // Event handlers to update the UI based on task events
    private void OnTaskCompleted(object sender, TaskCompletedEventArgs e)
    {
        TaskMessages.Add($"Task {e.TaskId} completed successfully.");
        var taskInfo = ActiveTasks.FirstOrDefault(t => t.TaskId == e.TaskId);
        if (taskInfo != null)
        {
            ActiveTasks.Remove(taskInfo);
        }
    }

    private void OnTaskErrored(object sender, TaskErrorEventArgs e)
    {
        ErrorMessages.Add($"Task {e.TaskId} encountered an error: {e.Error.Message}");
        var taskInfo = ActiveTasks.FirstOrDefault(t => t.TaskId == e.TaskId);
        if (taskInfo != null)
        {
            ActiveTasks.Remove(taskInfo);
        }
    }

    private void OnBatchCompleted(object sender, BatchCompletedEventArgs e)
    {
        TaskMessages.Add($"Batch task {e.TaskId} completed.");
        var taskInfo = ActiveTasks.FirstOrDefault(t => t.TaskId == e.TaskId);
        if (taskInfo != null)
        {
            ActiveTasks.Remove(taskInfo);
        }
    }

    private void OnItemProcessed(object sender, ItemProcessedEventArgs e)
    {
        if (e.Success)
        {
            TaskMessages.Add($"Item {e.Item} processed successfully in task {e.TaskId}.");
            var taskInfo = ActiveTasks.FirstOrDefault(t => t.TaskId == e.TaskId);
            if (taskInfo != null)
            {
                ActiveTasks.Remove(taskInfo);
            }
        }
        else
        {
            ErrorMessages.Add($"Item {e.Item} failed in task {e.TaskId}: {e.Error?.Message}");
            var taskInfo = ActiveTasks.FirstOrDefault(t => t.TaskId == e.TaskId);
            if (taskInfo != null)
            {
                ActiveTasks.Remove(taskInfo);
            }
        }
    }
}
public class TaskInfo
{
    public Guid TaskId { get; set; }
    public string Name { get; set; }
    public ICommand CancelCommand { get; set; }
}