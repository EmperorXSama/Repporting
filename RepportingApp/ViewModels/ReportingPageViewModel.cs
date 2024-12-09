
using System.Diagnostics;
using ClosedXML.Excel;
using Microsoft.VisualBasic;
using Microsoft.WindowsAPICodePack.Dialogs;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using Reporting.lib.Models.DTO;
using RepportingApp.CoreSystem.ApiSystem;

using RepportingApp.Services;
using RepportingApp.ViewModels.BaseServices;

namespace RepportingApp.ViewModels;

public partial class ReportingPageViewModel : ViewModelBase, ILoadableViewModel
{
    private bool _isFirstVisit = true;
    [ObservableProperty] private bool _isLoading;

    #region Multithread

    private UnifiedTaskManager _taskManager;

    // Observable collections to track task and item progress
    public ObservableCollection<string> TaskMessages { get; } = new();
    public ObservableCollection<string> ErrorMessages { get; } = new();


    [ObservableProperty] private Guid _cancelUploadToken;
    [ObservableProperty] private string _remainingOpenTasks;

    private readonly SystemConfigurationEstimator _configEstimator;
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



    [ObservableProperty] private bool _isReportingChoiceSelected;
    [ObservableProperty] private bool _isMMRIChoiceSelected;
    [ObservableProperty] private bool _isCollectDataSelected;
    [ObservableProperty] private bool _isArchiveChoiceSelected;
  

    [ObservableProperty]
    private ObservableCollection<string> _reportingselectedProcessesToIcon = new ObservableCollection<string>();

    [ObservableProperty] public ErrorIndicatorViewModel _errorIndicator = new ErrorIndicatorViewModel();
    
    // process selector popup
    [ObservableProperty] private bool _isCampaignSelected = false;



    #endregion

    #region Logic

    [ObservableProperty] private ReportingSettingValues _reportingSettingsValuesDisplay;
    [ObservableProperty] private ProxySittings _selectedProxySetting;
    [ObservableProperty] private ReportingSettings _selectedReportSetting;
    [ObservableProperty] private ReportingSittingsProcesses _selectedProcessesUi = new ReportingSittingsProcesses();
    private Dictionary<string, Func<EmailAccount, Task<ReturnTypeObject>>> _processsMapping;
    public List<string> SelectedProcesses { get; set; } = new();
    
    #endregion

    #region Upload Files/Data

    [ObservableProperty] private bool _isUploadPopupOpen = false;
    [ObservableProperty] private string _filePath;
    [ObservableProperty] private int _uploadProgress;
    [ObservableProperty] private string _fileName = "file name";
    [ObservableProperty] private Double _fileSize = 0;
    [ObservableProperty] private bool _isUploading;
    private CancellationTokenSource _cancellationTokenSource;
    [ObservableProperty] private ObservableCollection<EmailAccount>? _emailAccounts = new();

    #endregion

    #region Email Service

    private readonly IEmailAccountServices _emailAccountServices;

    #endregion

    #region mark messages as read & spam UI variables

    [ObservableProperty] private MarkMessagesAsReadConfig _markMessagesAsReadConfig = new MarkMessagesAsReadConfig();
    [ObservableProperty] private MarkMessagesAsReadConfig _markMessagesAsNotSpamConfig = new MarkMessagesAsReadConfig();
    [ObservableProperty] private MarkMessagesAsReadConfig _archiveMessagesConfig = new MarkMessagesAsReadConfig();
    #endregion
    #region Collect Directories Messages Variabless

        [ObservableProperty] private string _folderId = Statics.InboxDir;
        [ObservableProperty] private string _pathToSaveCountFile =  Statics.GetDesktopFilePath();
        [ObservableProperty] private string _subjectFileName = "SubjectsCollector";
        [ObservableProperty] private string _pathToSaveSubjectFile = Statics.GetDesktopFilePath();
        [ObservableProperty] private string _countFileName = "CountCollector";
        [ObservableProperty] private bool _isGenerateNumberCollectionRequested = true;
        [ObservableProperty] private bool _isGenerateCsvSubjectTableRequested = true;
        public ObservableCollection<KeyValuePair<string, string>> Folders { get; } = 
            new()
            {
                new KeyValuePair<string, string>("Inbox", Statics.InboxDir),
                new KeyValuePair<string, string>("Spam", Statics.SpamDir),
                new KeyValuePair<string, string>("Draft", Statics.DraftDir),
                new KeyValuePair<string, string>("Sent", Statics.SentDir),
                new KeyValuePair<string, string>("Trash", Statics.TrashDir),
                new KeyValuePair<string, string>("Archive", Statics.ArchiveDir),
            };
        
        [ObservableProperty]
        private KeyValuePair<string, string> _selectedFolder;
        
        partial void OnSelectedFolderChanged(KeyValuePair<string, string> value)
        {
            FolderId = value.Value; 
        }
    #endregion
    private readonly TaskInfoManager _taskInfoManager;
    [ObservableProperty] private int _tasksCount;
    public ObservableCollection<TaskInfoUiModel?> ActiveTasks => _taskInfoManager.GetTasks(TaskCategory.Active);

    public ObservableCollection<TaskInfoUiModel?> NotificationTasks =>
        _taskInfoManager.GetTasks(TaskCategory.Notification);

    public ObservableCollection<TaskInfoUiModel?> CampaignTasks => _taskInfoManager.GetTasks(TaskCategory.Campaign);

    #region API

    private readonly IApiConnector _apiConnector;
    private readonly IReportingRequests _reportingRequests;
    
    #endregion

    public ReportingPageViewModel(IMessenger messenger,
        SystemConfigurationEstimator configEstimator,
        TaskInfoManager taskInfoManager,
        IEmailAccountServices emailAccountServices,
        IApiConnector apiConnector,
        IReportingRequests reportingRequests) : base(messenger)
    {
        _apiConnector = apiConnector;
        _reportingRequests = reportingRequests;
        _emailAccountServices = emailAccountServices;
        _taskInfoManager = taskInfoManager;
        _configEstimator = configEstimator;

        InitializeSettings();
        InitializeTaskManager();
        InitializeCommands();
        SubscribeToEvents();


        TasksCount = _taskInfoManager.GetTasksCount();

    }

    #region Initial Setup and load
    private void InitializeSettings()
    {
        ReportingSettingsValuesDisplay = new ReportingSettingValues(App.Configuration);
        AssignThreadDefaultValues();
        InitializeProcessMappings();
    }

    private void InitializeTaskManager()
    {
        var systemEstimator = new SystemConfigurationEstimator();
        _taskManager = new UnifiedTaskManager(systemEstimator.RecommendedMaxDegreeOfParallelism, _taskInfoManager);
    }

    private void InitializeCommands()
    {
        // Set up your commands here (if any).
    }

    private void SubscribeToEvents()
    {
        _taskInfoManager.PropertyChanged += TaskManager_PropertyChanged;
        SubscribeEventToThread();
    }
    private void InitializeProcessMappings()
    {
        _processsMapping = new Dictionary<string, Func<EmailAccount, Task<ReturnTypeObject>>>
        {
            {"IsReportingSelected", async (emailAcc) =>
                {
                    if (emailAcc == null) throw new ArgumentNullException(nameof(emailAcc));
                    ReturnTypeObject result = await _reportingRequests.ProcessMarkMessagesAsNotSpam(emailAcc,MarkMessagesAsNotSpamConfig);
                    return result; 
                }
            },
            {"MarkMessagesAsReadFromInbox",async (emailAcc) =>
                {
                    if (emailAcc == null) throw new ArgumentNullException(nameof(emailAcc));
                    return await _reportingRequests.ProcessMarkMessagesAsReadFromDir(emailAcc,MarkMessagesAsReadConfig,FolderId);
                }
            },
            {"CollectMessagesCount",async (emailAcc) =>
                {
                    if (emailAcc == null) throw new ArgumentNullException(nameof(emailAcc));
                    return await _reportingRequests.ProcessGetMessagesFromDir(emailAcc,
                        FolderId);
                }
            },
            {"ArchiveMessages",async (emailAcc) =>
                {
                    if (emailAcc == null) throw new ArgumentNullException(nameof(emailAcc));
                    return await _reportingRequests.ProcessArchiveMessages(emailAcc,ArchiveMessagesConfig);
                }
            },
        };
    }
    public async Task LoadDataIfFirstVisitAsync()
    {
        try
        {
            if (!IsLoading)
            {
                IsLoading = true;
                // Load data here
                await LoadDataAsync();
            }
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Loading Data Failed", e.Message);
        }
        finally
        {
            IsLoading = false;
        }

    }

    private async Task LoadDataAsync()
    {
        var groups = await _apiConnector.GetDataAsync<IEnumerable<EmailGroup>>(ApiEndPoints.GetGroups);
        var emails = await _apiConnector.GetDataAsync<IEnumerable<EmailAccount>>(ApiEndPoints.GetEmails);
        Groups = groups.ToObservableCollection();
        EmailAccounts = emails.ToObservableCollection();
        Debug.WriteLine("loading data finished");
    }

    #endregion



    #region Relay commands

    
    [RelayCommand]
    private async Task StartOperation()
    {
        var modifier = GetStartProcessNotifierModel();
        _messenger.Send(new ProcessStartMessage("Success", "Reporting", modifier));
        // Example usage: Start a single task
        var task1 = _taskManager.StartTask(async cancellationToken =>
        {
            // Simulate some asynchronous work
            await Task.Delay(155000, cancellationToken);

        });
            await CreateAnActiveTask(TaskCategory.Active, TakInfoType.Single, task1, "Preparation", Statics.UploadFileColor,
                Statics.UploadFileSoftColor,EmailAccounts);
       
        // Example usage: Start a batch process
        var task2 = _taskManager.StartBatch(EmailAccounts, async (item, cancellationToken) =>
        {
            // Simulate processing each item
            await Task.Delay(2500, cancellationToken);
            if (item.Group.GroupId == 12) throw new Exception("Simulated failure"); // Simulate error
            return "";
        }, batchSize: 3);
   
        await CreateAnActiveTask(TaskCategory.Active, TakInfoType.Batch, task2, "Repporting", Statics.ReportingColor,
                Statics.ReportingSoftColor,EmailAccounts);
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        await ReportingSettingsValuesDisplay.SaveConfigurationAsync();
    }

    [RelayCommand]
    private void ProcessesCheckedManager(string value)
    {

        #region this logic used to represent the processed selected in UI ( UI CODE ONLY)

        var processIconMapping = new Dictionary<Func<ReportingSittingsProcesses, bool>, string>
        {
            { p => p.IsReportingSelected, "ReportingIcon" },
            { p => p.IsDeleteSpamSelected, "DeleteSpamIcon" },
            { p => p.IsGetSpamNumbersSelected, "CollectSpamNumberIcon" },
            { p => p.IsGetSpamSubjectSelected, "CollectSpamSubjectIcon" },
            { p => p.IsFixAfterFinish, "FixIcon" },
            { p => p.IsMMRI, "FixIcon" },

        };
        IsReportingChoiceSelected = SelectedProcessesUi.IsReportingSelected;
        IsMMRIChoiceSelected = SelectedProcessesUi.IsMMRI;
        IsCollectDataSelected = SelectedProcessesUi.IsGetSpamNumbersSelected;
        IsArchiveChoiceSelected = SelectedProcessesUi.IsArchiveSelected;
        foreach (var mapping in processIconMapping)
        {
            var isSelected = mapping.Key(SelectedProcessesUi);
            var iconName = mapping.Value;

            if (isSelected)
            {

                if (!ReportingselectedProcessesToIcon.Contains(iconName)) // Corrected the collection reference
                {
                    ReportingselectedProcessesToIcon.Add(iconName);
                }
            }
            else
            {

                if (ReportingselectedProcessesToIcon.Contains(iconName))
                {
                    ReportingselectedProcessesToIcon.Remove(iconName);
                }
            }
        }

        #endregion

        #region This Code is storing the proceses that will be processed ( LOGIC ONLY )
        
        // todo : if reporting ( mark not spam selected then remove all pre reporting singularities if exist )
        if (SelectedProcesses.Contains(value))
        {
            SelectedProcesses.Remove(value);
        }
        else
        {
            SelectedProcesses.Add(value);
        }
        #endregion
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
        }
        else if (IsRandom)
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
        }
        else if (IsAll)
        {
            SelectedReportSetting = ReportingSettings.All;
        }
    }

    [RelayCommand]
    private void OpenPopup()   {
        IsCampaignSelected = false;
        IsPopupOpen = true;
    }

    [RelayCommand]
    private void ClosePopup() => IsPopupOpen = false;

    [RelayCommand]
    private void OpenCampaignPopup()
    {
        IsCampaignSelected = true;
        IsPopupOpen = true;
    }
    

    [RelayCommand]
    private void OpenNotificationopup() => IsNotificationOpen = !IsNotificationOpen;

    [RelayCommand]
    private async Task ReconnectToApi()
    {
        await LoadDataIfFirstVisitAsync();
    }


    #endregion

    #region Group Selection and creation

    [ObservableProperty] private bool _isNewGroupSelected;
    [ObservableProperty] private bool _isExistingGroupSelected;
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private bool _isGroupSettingsDropdownOpen = false;
    [ObservableProperty] private bool _isGroupSettingsDropdownOpen1 = false;
    
    [RelayCommand] private void ToggleGroupSelctionMenu() => IsGroupSettingsDropdownOpen = !IsGroupSettingsDropdownOpen; 
    [RelayCommand] private void ToggleGroupSelctionMenuOne() => IsGroupSettingsDropdownOpen1 = !IsGroupSettingsDropdownOpen1; 

    [ObservableProperty] private ObservableCollection<EmailGroup> _groups= new ObservableCollection<EmailGroup>();

    [ObservableProperty] private string _newGroupName;
    [ObservableProperty] private EmailGroup? _selectedEmailGroup = new EmailGroup(); 
    [ObservableProperty] private ObservableCollection<EmailGroup> _selectedEmailGroupForTask = new (); 
    
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
            
            var id = await _apiConnector.PostDataObjectAsync<int>(ApiEndPoints.PostGroup,NewGroupName);
            EmailGroup newGroup = new EmailGroup()
            {
                GroupId = id,
                GroupName = NewGroupName
            };
            Groups.Add(newGroup);
            IsEnabled = true;
            SelectedEmailGroup = newGroup;
            
            NewGroupName = string.Empty;
           
        }
        else
        {
            IsEnabled = true;
        }
    }
    #endregion

    #region Upload File/Data Func
    private readonly ConcurrentBag<CreateEmailAccountDto> _emailsToGetUploaded = new();

    
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
                ErrorIndicator = new ErrorIndicatorViewModel();
                await ErrorIndicator.ShowErrorIndecator("Invalid File", "You dropped an invalid file extention (only txt, excel format)");
                return;
            }

            if (SelectedEmailGroup?.GroupName == null)
            {
                ErrorIndicator = new ErrorIndicatorViewModel();
                await ErrorIndicator.ShowErrorIndecator("no group selected", "you have to assign a group or give a name for a group to be created ");
                return;
            }
            
            var fileInfo = new FileInfo(filePath);
            FileName = fileInfo.Name;
            FileSize = Math.Floor(fileInfo.Length / (1024.0 * 1024.0)); // Convert to MB

            _cancellationTokenSource = new CancellationTokenSource();
            IsUploading = true;
            UploadProgress = 0;

            // Reset _processedLines for each new file upload
            _processedLines = 0;
        

            var lines = await File.ReadAllLinesAsync(filePath);
            var totalLines = lines.Length;
            var progress = new Progress<int>(value => UploadProgress = value);

            var currentTaskId = _taskManager.StartBatch(
                lines, 
                (line, cancellationToken) => ProcessLineWithProgress(line, totalLines, progress, cancellationToken), 
                batchSize: _configEstimator.RecommendedBatchSize
            );  
        
        
            await CreateAnActiveTask(TaskCategory.Active, TakInfoType.Batch, currentTaskId, "File Upload", Statics.UploadFileColor, Statics.UploadFileSoftColor,null);
            await _taskManager.WaitForTaskCompletion(currentTaskId);
            
            int? groupId = SelectedEmailGroup.GroupId; 
            string? groupName = SelectedEmailGroup.GroupName;
            
            var payload = new
            {
                emailAccounts = _emailsToGetUploaded,
                groupId,
                groupName
            };
            
            var apiCreateEmails = _taskManager.StartTask(async cancellationToken =>
            {
             
                await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.GetAddEmails, payload);
                /*_taskManager.UpdateUiThreadValues(
                    ()=>   Groups.Add(newGroup),
                    ()=>   IsEnabled = true,
                    ()=>    SelectedEmailGroup = newGroup);*/
             
            });
            
            await CreateAnActiveTask(TaskCategory.Active, TakInfoType.Single, apiCreateEmails, "Create Emails (API)", Statics.UploadFileColor, Statics.UploadFileSoftColor,null);
            await _taskManager.WaitForTaskCompletion(apiCreateEmails);
            _emailsToGetUploaded.Clear();
    }
    private int _processedLines;

    private async Task<string> ProcessLineWithProgress(
        string line,
        int totalLines,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var data = line.Split(';');
        if (data.Length != 7)
        {
            // Handle incorrect data format, e.g., skip the line or log an error
            return "";
        }

        var emailAccount = new CreateEmailAccountDto
        {
            EmailAddress = data[0],
            Password = data[1],
            RecoveryEmail = data[2],
            Proxy = new ProxyDto
            {
                ProxyIp = data[3],
                Port = int.TryParse(data[4], out var port) ? port : 0,
                Username = data[5],
                Password = data[6]
            },
            Status = EmailStatus.NewAdded,
            Group = SelectedEmailGroup,
        };
        
        await Dispatcher.UIThread.InvokeAsync(
            () => _emailsToGetUploaded.Add(emailAccount));

    // Update and report progress
        int percentComplete = (int)((Interlocked.Increment(ref _processedLines) * 100.0) / totalLines);
        progress.Report(percentComplete);
        return "";
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
    
    public async Task  CreateAnActiveTask(TaskCategory category, TakInfoType type, Guid taskId, string name, string color, string softColor,ObservableCollection<EmailAccount>? assignedGroupEmails)
    {
        await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
        {
            var taskInfo = new TaskInfoUiModel(type)
            {
                TaskId = taskId,
                Name = name,
                Color = color,
                SoftColor = softColor,
                AssignedEmails = assignedGroupEmails,
                AssignedEmailsDisplayInfo = assignedGroupEmails,
                CancelCommand = new RelayCommand(() =>
                {
                    CancelTask(taskId);
                    _taskInfoManager.CompleteTask(taskId,category);
                    TasksCount = _taskInfoManager.GetTasksCount();
                })
            };

            _taskInfoManager.AddTask(category, taskInfo);
            TasksCount = _taskInfoManager.GetTasksCount();
        });
       
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
        TasksCount = _taskInfoManager.GetTasksCount();
    } 
    private void OnCampaignTasksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        
        RemainingOpenTasks = $"{RecommendedMaxDegreeOfParallelismDisplay -  _taskInfoManager.GetTasksCount()} Open";
        TasksCount = _taskInfoManager.GetTasksCount();
    } 
    private void OnTaskCompleted(object? sender, TaskCompletedEventArgs e)
    {
        TaskMessages.Add($"Task {e.TaskId} completed successfully.");
        _taskInfoManager.CompleteTask(e.TaskId);
        TasksCount = _taskInfoManager.GetTasksCount();
    }
    
    private void OnTaskErrored(object? sender, TaskErrorEventArgs e)
    {
        ErrorMessages.Add($"Task {e.TaskId} encountered an error: {e.Error.Message}");
        _taskInfoManager.CompleteTask(e.TaskId);
        ErrorIndicator = new ErrorIndicatorViewModel();
        ErrorIndicator.ShowErrorIndecator("Invalid File", e.Error.Message);
        TasksCount = _taskInfoManager.GetTasksCount();
    }

    private void OnBatchCompleted(object? sender, BatchCompletedEventArgs e)
    {
        TaskMessages.Add($"Batch task {e.TaskId} completed.");
        _taskInfoManager.CompleteTask(e.TaskId);
        TasksCount = _taskInfoManager.GetTasksCount();
    }

private void OnItemProcessed(object? sender, ItemProcessedEventArgs e)
{
    TaskInfoUiModel? taskInfo = _taskInfoManager.GetTasks(TaskCategory.Active)
        .FirstOrDefault(t => t.TaskId == e.TaskId);
    if (taskInfo == null)
    {
        taskInfo = _taskInfoManager.GetTasks(TaskCategory.Campaign)
            .FirstOrDefault(t => t.TaskId == e.TaskId);
    }
    if (e.Success)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (e.Item is EmailAccount emailProcessed)
            {
                taskInfo.ItemSuccesMessasges.Add(new ItemInfo()
                {
                    Email = emailProcessed,
                    Title = "Success",
                    Message = e.Message
                });
                TaskMessages.Add($"Email processed successfully in task {e.TaskId}.");
            }
            else
            {
                taskInfo.ItemSuccesMessasges.Add(new ItemInfo()
                {
                    
                    Message = $"Non-email item processed successfully in task {e.TaskId}: {e.Item}"
                });
                TaskMessages.Add($"Non-email item processed successfully in task {e.TaskId}: {e.Item}");
            }
        });
    }
    else
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (e.Item is EmailAccount emailProcessed)
            {
                taskInfo.ItemFailedMessasges.Add(new ItemInfo()
                {
                    Email = emailProcessed,
                    Title = e.Error?.Message.GetValueBetweenBrackets() ,
                    Message = $"Email failed in task {e.TaskId}: {e.Error?.Message}"
                });
                ErrorMessages.Add($"Email failed in task {e.TaskId}: {e.Error?.Message}");
               
            }
            else
            {
                taskInfo.ItemFailedMessasges.Add(new ItemInfo()
                {
                    Message = $"Non-email item failed in task {e.TaskId}: {e.Error?.Message}"
                });
                ErrorMessages.Add($"Non-email item failed in task {e.TaskId}: {e.Error?.Message}");
            }
        });
    }
}

    
    #endregion

    #region Main reporting logic
    public ObservableCollection<EmailAccount> FilterEmailsBySelectedGroups()
    {
        if (!SelectedEmailGroupForTask.Any())
        {
            // If no groups are selected, return an empty collection or the full list, depending on your requirements
            return new ObservableCollection<EmailAccount>();
        }

        var filteredEmails = EmailAccounts
            ?.Where(email => SelectedEmailGroupForTask.Any(group => group.GroupId == email.Group.GroupId))
            .ToList();

        return new ObservableCollection<EmailAccount>(filteredEmails!.ToObservableCollection());
    }

    #region campaign
    
    [RelayCommand]
    private async Task ReadAllEmailsInboxMessages()
    {
        if (!SelectedProcesses.Any())
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Issue", "No process has been selected.");
            return;
        }
        var emailsGroupToWork = FilterEmailsBySelectedGroups();
        if (!emailsGroupToWork.Any())
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Issue", "The group selected contains no emails.");
            return;
        }

        if (SelectedProcesses[0] == null)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Selection Issue", "The process Selected have no correspondent Logic.");
            return;
        }

        ReturnTypeObject result = new ReturnTypeObject(){Message = "Nothing inside"};
        foreach (var processName in SelectedProcesses)
        {
            var taskId = _taskManager.StartBatch(emailsGroupToWork, async (emailAccount, cancellationToken) =>
            {
                if (_processsMapping.TryGetValue(processName, out var processFunction))
                {
                     result=  await processFunction(emailAccount);
                     switch (processName)
                     {
                         case "CollectMessagesCount":
                             await PostCollectMessages(emailAccount.EmailAddress,(ObservableCollection<FolderMessages>)result.ReturnedValue);
                             break;
                         default:
                         break;
                     }
                }
                
                return result.Message;
            });
            await CreateAnActiveTask(TaskCategory.Active,TakInfoType.Batch,taskId,processName,Statics.ReportingColor,Statics.ReportingSoftColor,emailsGroupToWork);
            await _taskManager.WaitForTaskCompletion(taskId);
        }
       
       
    }

    #region post processors

    #region collect data
    private List<(string Email, ObservableCollection<FolderMessages> Messages)> allMessages = new();

    #region relay commands

  

    #endregion
    private async Task PostCollectMessages(string email,ObservableCollection<FolderMessages> inboxMessages)
    {
        allMessages.Add((email, inboxMessages));
        var emailMessages = allMessages
            .Select(account => (account.Email, account.Messages))
            .ToList();
        if (IsGenerateNumberCollectionRequested)
        {
            await GenerateNumberCollection();
        }

        if (IsGenerateCsvSubjectTableRequested)
        {
            await GenerateCsvSubjectTableMultithreadAsync(emailMessages);
        }
    }
    private async Task GenerateNumberCollection()
    {
        var filePath = Path.Combine(PathToSaveCountFile, $"{CountFileName}.xlsx");

        // Step 1: Lock access to the file
        lock (_fileLock)
        {
            // Step 2: Create a new Excel workbook and worksheet
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.AddWorksheet("Message Count");

                // Step 3: Add headers
                worksheet.Cell(1, 1).Value = "Email";
                worksheet.Cell(1, 2).Value = "MessageCount";

                int row = 2; // Start from the second row
                foreach (var (email, messages) in allMessages)
                {
                    if (messages.Any())
                    {
                        worksheet.Cell(row, 1).Value = email;
                        worksheet.Cell(row, 2).Value = messages[0].folder.total;
                    }
                    else
                    {
                        worksheet.Cell(row, 1).Value = email;
                        worksheet.Cell(row, 2).Value = 0;
                    }
                    row++;
                }

                // Step 4: Save the workbook to the file
                workbook.SaveAs(filePath);
            }
        }

        // Simulate async operation
        await Task.CompletedTask;
    }



   private readonly object _fileLock = new(); // Synchronization object

private ConcurrentDictionary<string, Dictionary<string, int>> _subjectCounts = new(); // Shared memory buffer

private async Task ProcessEmailSubjects(string email, ObservableCollection<FolderMessages> messages)
{
    // Step 1: Process messages for the current email
    var subjectCounts = messages
        .GroupBy(msg => msg.headers.subject)
        .ToDictionary(
            g => g.Key,   // Subject
            g => g.Count() // Count of messages with this subject
        );

    // Step 2: Add to shared memory (thread-safe with ConcurrentDictionary)
    _subjectCounts[email] = subjectCounts;

    await Task.CompletedTask; // Simulate async processing
}

private async Task SaveSubjectCountsToExcelAsync(string filePath)
{
    // Step 3: Lock access to the file
    lock (_fileLock)
    {
        // Initialize Excel workbook
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Subject Counts");

        // Add headers
        worksheet.Cell(1, 1).Value = "Email";
        int columnIndex = 2;

        // Collect unique subjects across all emails to create dynamic columns
        var uniqueSubjects = _subjectCounts
            .SelectMany(emailGroup => emailGroup.Value.Keys)
            .Distinct()
            .ToList();

        foreach (var subject in uniqueSubjects)
        {
            worksheet.Cell(1, columnIndex++).Value = subject;
        }

        worksheet.Cell(1, columnIndex).Value = "Total"; // Add a 'Total' column

        // Add data rows
        int rowIndex = 2;
        foreach (var emailGroup in _subjectCounts)
        {
            worksheet.Cell(rowIndex, 1).Value = emailGroup.Key; // Email

            columnIndex = 2;
            int total = 0;

            foreach (var subject in uniqueSubjects)
            {
                // Get count for the subject or 0 if not present
                int count = emailGroup.Value.TryGetValue(subject, out var countValue) ? countValue : 0;
                worksheet.Cell(rowIndex, columnIndex++).Value = $"x{count}";
                total += count;
            }

            worksheet.Cell(rowIndex, columnIndex).Value = $"x{total}"; // Total count
            rowIndex++;
        }

        // Save file
        workbook.SaveAs(filePath);
    }
    await Task.CompletedTask;
}

public async Task GenerateCsvSubjectTableMultithreadAsync(List<(string Email, ObservableCollection<FolderMessages> Messages)> allMessages)
{
    // Prepare the file path
    string filePath = Path.Combine(PathToSaveSubjectFile, $"{SubjectFileName}.xlsx");

    // Step 1: Process each email in parallel
    var tasks = allMessages
        .Select(account => Task.Run(() => ProcessEmailSubjects(account.Email, account.Messages)))
        .ToList();

    await Task.WhenAll(tasks);

    // Step 2: Save all results to the Excel file
    await SaveSubjectCountsToExcelAsync(filePath);
}


    #endregion

    #endregion
    [RelayCommand]
    public async Task AddSingleCampaignTask()
    {
        TimeSpan interval = TimeSpan.FromSeconds(ReportingSettingsValuesDisplay.TimeUntilNextRun);
        var modifier = GetStartProcessNotifierModel();
        if (!SelectedProcesses.Any())
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Issue", "No process has been selected.");
            return;
        }
        var emailsGroupToWork = FilterEmailsBySelectedGroups();
        if (!emailsGroupToWork.Any())
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Issue", "The group selected contains no emails.");
            return;
        }

        if (SelectedProcesses[0] == null)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Selection Issue", "The process Selected have no correspondent Logic.");
            return;
        }

        ReturnTypeObject result = new ReturnTypeObject(){Message = "Nothing inside"};
        foreach (var processName in SelectedProcesses)
        {
            var taskId = _taskManager.StartLoopingTaskBatch(emailsGroupToWork, async (emailAccount, cancellationToken) =>
            {
                if (_processsMapping.TryGetValue(processName, out var processFunction))
                {
                    result=  await processFunction(emailAccount);
                    switch (processName)
                    {
                        case "CollectMessagesCount":
                            await PostCollectMessages(emailAccount.EmailAddress,(ObservableCollection<FolderMessages>)result.ReturnedValue);
                            break;
                        default:
                            break;
                    }
                }
                
                return result.Message;
            }, ReportingSettingsValuesDisplay.Thread,interval);
            await CreateAnActiveTask(TaskCategory.Campaign,TakInfoType.Single,taskId,"reporting",Statics.UploadFileColor,Statics.UploadFileSoftColor,EmailAccounts);
            await _taskManager.WaitForTaskCompletion(taskId);
        }
        
       
       
    }
    #endregion
    private StartProcessNotifierModel GetStartProcessNotifierModel()
    {
        var modifier = new StartProcessNotifierModel()
        {
            ReportingSettingsP = SelectedProcessesUi,
            Thread = ReportingSettingsValuesDisplay.Thread,
            Repetition = ReportingSettingsValuesDisplay.Repetition,
            RepetitionDelay = ReportingSettingsValuesDisplay.RepetitionDelay,
            SelectedProxySetting =  this.SelectedProxySetting,
            SelectedReportSetting =  this.SelectedReportSetting,
            Interval = ReportingSettingsValuesDisplay.TimeUntilNextRun
        };
        return modifier;
    }
    #endregion
    
   
}


