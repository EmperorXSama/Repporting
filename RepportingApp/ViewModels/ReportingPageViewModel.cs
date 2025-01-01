
using System.Diagnostics;
using ClosedXML.Excel;
using DataAccess.Enums;
using Microsoft.VisualBasic;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using Reporting.lib.Data.Services.Emails;
using Reporting.lib.Models.DTO;
using RepportingApp.CoreSystem.ApiSystem;
using RepportingApp.CoreSystem.FileSystem;
using RepportingApp.CoreSystem.ProxyService;
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
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Loading Data Failed", e.Message);
        }
        finally
        {
            IsLoading = false;
        }

    }

    private async Task LoadDataAsync(bool ignoreCache)
    {
        var groups = await _apiConnector.GetDataAsync<IEnumerable<EmailGroup>>(ApiEndPoints.GetGroups, ignoreCache:ignoreCache);
        var emails = await _apiConnector.GetDataAsync<IEnumerable<EmailAccount>>(ApiEndPoints.GetEmails,ignoreCache:ignoreCache);
        //FileManager.WriteEmailAccountsToFileAsync("emails.txt",emails);
        Groups = groups.ToObservableCollection();
        
        NetworkItems = emails.ToObservableCollection();
        EmailDiaplysTable = new ObservableCollection<EmailAccount>(NetworkItems);
        EmailAccounts =  emails.ToObservableCollection();
        CountFilter = EmailDiaplysTable.Count;
    }

    #endregion



    #region Relay commands



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
        await LoadDataIfFirstVisitAsync(true);
    }


    #endregion

    #region Group Selection and creation

    [ObservableProperty] private bool _isNewGroupSelected;
    [ObservableProperty] private bool _isExistingGroupSelected;
    [ObservableProperty] private bool _isDeleteGroupSelected;
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
        IsDeleteGroupSelected = false;
        SelectedEmailGroup = new EmailGroup();
    }

    [RelayCommand]
    private void SelectExistingGroup()
    {
        IsNewGroupSelected = false;
        IsDeleteGroupSelected = false;
        IsExistingGroupSelected = true;
    }
    [RelayCommand]
    private void SelectDeleteGroup()
    {
        IsNewGroupSelected = false;
        IsDeleteGroupSelected = true;
        IsExistingGroupSelected = false;
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
    [RelayCommand]
    private async Task DeleteGroup()
    {
        IsEnabled = false;

        try
        {
            if (SelectedEmailGroup == null)
            {
                throw new Exception("You must select a group to delete");
            }
            var result = await _apiConnector.PostDataObjectAsync<bool>(ApiEndPoints.DeleteGroup, SelectedEmailGroup.GroupId!);
            if (!result) throw new Exception("Delete group failed");
            await ReconnectToApi();

        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("no group deleted",
                e.Message);
        }
        finally
        {
            IsEnabled = true;
            SelectedEmailGroup = null;
        }
            
       
            
    }
    #endregion

    #region Upload File/Data Func
    private int _processedLines;
    private readonly ConcurrentBag<CreateEmailAccountDto> _emailsToGetUploaded = new();
    private readonly ConcurrentDictionary<string, EmailMetadataDto> _metadataDictionary = new();
    private readonly object _emailLock = new();
    private readonly object _metadataLock = new();
    
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
        try
        {
            _emailsToGetUploaded.Clear();
            _metadataDictionary.Clear();
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
            _processedLines = 0;
        

            var fileLines =  File.ReadLines(filePath);
            var lines2 = File.ReadLines(FileManager.GetMasterIdsPath());
             var totalLines = fileLines.Count();
            var progress = new Progress<int>(value => UploadProgress = value);

            
            var batches = fileLines.Batch(1000);
            foreach (var batch in batches)
            {
                var currentBatchId = _taskManager.StartBatch(
                    batch,
                    (line, cancellationToken) => ProcessLineWithProgress(line, totalLines, progress, cancellationToken),
                    TaskCategory.Invincible,
                    batchSize: _configEstimator.RecommendedBatchSize
                );
                await CreateAnActiveTask(TaskCategory.Invincible, TakInfoType.Batch, currentBatchId, "profile Upload", Statics.UploadFileColor, Statics.UploadFileSoftColor,null);
                await _taskManager.WaitForTaskCompletion(currentBatchId);
            }
            
            var Metabatches = lines2.Batch(1000);
            foreach (var batch in Metabatches)
            {
                var currentTaskId4 = _taskManager.StartBatch(
                    batch, 
                    async (line, cancellationToken) => await ProcessMetaData(line, cancellationToken), 
                    TaskCategory.Invincible,
                    batchSize: _configEstimator.RecommendedBatchSize
                    
                );  
                await CreateAnActiveTask(TaskCategory.Invincible, TakInfoType.Batch, currentTaskId4, "MetaData Upload", Statics.UploadFileColor, Statics.UploadFileSoftColor,null);
                await _taskManager.WaitForTaskCompletion(currentTaskId4);
            }
            
            int? groupId = SelectedEmailGroup.GroupId; 
            string? groupName = SelectedEmailGroup.GroupName;
            var emailsSnapshot = new List<CreateEmailAccountDto>();
            var metadataSnapshot = new Dictionary<string, EmailMetadataDto>();
            
            emailsSnapshot = _emailsToGetUploaded.ToList();
            metadataSnapshot = _metadataDictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var unmatchedEmails = emailsSnapshot
                .Where(email => !metadataSnapshot.ContainsKey(email.EmailAddress))
                .ToList();

            if (unmatchedEmails.Any())
            {
                ErrorIndicator = new ErrorIndicatorViewModel();
                await ErrorIndicator.ShowErrorIndecator("no group selected", $"The following emails have no matching metadata: {string.Join(", ", unmatchedEmails.Select(e => e.EmailAddress))}");
                return;
            }
            // Define the chunk size
            const int chunkSize = 1000;
            var chunks = emailsSnapshot
                .Select((email, index) => new { email, index })
                .GroupBy(x => x.index / chunkSize)
                .Select(group => new
                {
                    Emails = group.Select(x => x.email).ToList(),
                    Metadata = group.Select(x => x.email.EmailAddress)
                        .ToDictionary(email => email, email => metadataSnapshot[email])
                })
                .ToList();

// Process each chunk
            foreach (var chunk in chunks)
            {
                var chunkPayload = new
                {
                    emailAccounts = chunk.Emails,
                    emailMetadata = chunk.Metadata,
                    groupId,
                    groupName
                };

                var apiCreateEmails = _taskManager.StartTask(async cancellationToken =>
                {
                    await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.GetAddEmails, chunkPayload);
                });

                await CreateAnActiveTask(TaskCategory.Active, TakInfoType.Single, apiCreateEmails, 
                    "Create Emails (API)", Statics.UploadFileColor, Statics.UploadFileSoftColor, null);

                await _taskManager.WaitForTaskCompletion(apiCreateEmails);
            }
            ReconnectToApi();
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("File", e.Message);
        }
           
    }
  

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

        // Add to the collection (no UI thread needed)
        _emailsToGetUploaded.Add(emailAccount);

        // Update and report progress
        int percentComplete = (int)((Interlocked.Increment(ref _processedLines) * 100.0) / totalLines);
        progress.Report(percentComplete);
        return "";
    }

    private async Task<string> ProcessMetaData(
        string line,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var data = line.Split(',');
        if (data.Length !=  4)
        {
            // Handle incorrect data format, e.g., skip the line or log an error
            return "";
        }
        var email = data[0];
        var emailAccount = new EmailMetadataDto
        {
            MailId = data[1],
            YmreqId = data[2],
            Wssid = data[3],
            Cookie = await File.ReadAllTextAsync(Path.Combine(FileManager.GetProfileCookieFolder(email + ".txt")), cancellationToken)
        };
        
       

        _metadataDictionary.TryAdd(email, emailAccount);
    // Update and report progress
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
                AssignedEmails = assignedGroupEmails != null ? new ObservableCollection<EmailAccount>(assignedGroupEmails) : new ObservableCollection<EmailAccount>(),
                AssignedEmailsDisplayInfo =  assignedGroupEmails != null ? new ObservableCollection<EmailAccount>(assignedGroupEmails) : new ObservableCollection<EmailAccount>(),
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

    private async void OnBatchCompleted(object? sender, BatchCompletedEventArgs e)
    {
        
        TaskMessages.Add($"Batch task {e.TaskId} completed.");
        TasksCount = _taskInfoManager.GetTasksCount();

        // Retrieve processed items
        if (e.ProcessedItems != null)
        {
            var processedEmails = e.ProcessedItems
                .OfType<EmailAccount>() // Filter only EmailAccount items
                .Where(email => email.Stats != null) // Ensure TAT is not null
                .ToList();

            if (processedEmails.Any())
            {
                try
                {
                   
                    // Batch update email stats
                    var apiCreateEmails = _taskManager.StartTask(async cancellationToken =>
                    {
                        var processedEmailsDto = processedEmails.Select(email => new EmailStatsUpdateDto
                        {
                            EmailAccountId = email.Id,
                            InboxCount = email.Stats.InboxCount,
                            SpamCount = email.Stats.SpamCount,
                            LastReadCount = email.Stats.LastReadCount,
                            LastArchivedCount = email.Stats.LastArchivedCount,
                            LastNotSpamCount = email.Stats.LastNotSpamCount,
                            UpdatedAt = DateTime.UtcNow
                        }).ToList();
                        await _apiConnector.PostDataObjectAsync<object>(
                            ApiEndPoints.UpdateStatsEmails, 
                            processedEmailsDto 
                        );
                        await Dispatcher.UIThread.InvokeAsync(
                            () =>
                            {
                                EmailDiaplysTable = NetworkItems;
                
                            });
                    });
                    
                    await CreateAnActiveTask(TaskCategory.Active,TakInfoType.Single,apiCreateEmails,"update api ",Statics.ReportingColor,Statics.ReportingSoftColor,null);
                    TaskMessages.Add($"Email stats updated for {processedEmails.Count} emails in task {e.TaskId}.");
                }
                catch (Exception ex)
                {
                    ErrorMessages.Add($"Failed to update email stats for task {e.TaskId}: {ex.Message}");
                }
            }
        }
    }


private void OnItemProcessed(object? sender, ItemProcessedEventArgs e)
{
    try
    {
        TaskInfoUiModel? taskInfo = _taskInfoManager.GetTasks(e.TaskCategoryName)
                .FirstOrDefault(t => t.TaskId == e.TaskId);

            if (taskInfo == null)
            {
               taskInfo = _taskInfoManager.GetTasks(TaskCategory.Notification)
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
                        string errorMessage = e.Error?.Message ?? "Unknown error";
                        if (errorMessage.Contains("connection attempt failed", StringComparison.OrdinalIgnoreCase) ||
                            errorMessage.Contains("established connection failed", StringComparison.OrdinalIgnoreCase))
                        {
                            errorMessage = "Proxy Error"; 
                        }
                       
                        taskInfo.ItemFailedMessasges.Add(new ItemInfo()
                        {
                            Email = emailProcessed,
                            Title = e.Error?.Message.GetValueBetweenBrackets() ,
                            Message = $"Email failed in task {e.TaskId}: {e.Error?.Message}"
                        });
                        ErrorMessages.Add($"Email failed in task {e.TaskId}: {e.Error?.Message}");
                        // Check if the error already exists
                        Dispatcher.UIThread.Post(() =>
                        {
                            var existingItem = taskInfo.UniqueErrorMessages.FirstOrDefault(x => x.Key == errorMessage);
                            if (existingItem.Key != null)
                            {
                                // Remove old item and add updated one
                                taskInfo.UniqueErrorMessages.Remove(existingItem);
                                taskInfo.UniqueErrorMessages.Add(new KeyValuePair<string, int>(errorMessage, existingItem.Value + 1));
                            }
                            else
                            {
                                // Add new item
                                taskInfo.UniqueErrorMessages.Add(new KeyValuePair<string, int>(errorMessage, 1));
                            }
                        });
                    }
                    else
                    {
                        taskInfo.ItemFailedMessasges.Add(new ItemInfo()
                        {
                            Message = $"Non-email item failed in task {e.TaskId}: {e.Error?.Message}"
                        });
                        ErrorMessages.Add($"Non-email item failed in task {e.TaskId}: {e.Error?.Message}");
                        string errorMessage = e.Error?.Message ?? "Unknown error";
                        Dispatcher.UIThread.Post(() =>
                        {
                            var existingItem = taskInfo.UniqueErrorMessages.FirstOrDefault(x => x.Key == errorMessage);
                            if (existingItem.Key != null)
                            {
                                // Remove old item and add updated one
                                taskInfo.UniqueErrorMessages.Remove(existingItem);
                                taskInfo.UniqueErrorMessages.Add(new KeyValuePair<string, int>(errorMessage, existingItem.Value + 1));
                            }
                            else
                            {
                                // Add new item
                                taskInfo.UniqueErrorMessages.Add(new KeyValuePair<string, int>(errorMessage, 1));
                            }
                        });
                     
                    }
                });
            }
    }
    catch (Exception exception)
    {
        Console.WriteLine(exception);
        throw;
    }
    
}

    
    #endregion

    #region Main reporting logic
    public ObservableCollection<EmailAccount> FilterEmailsBySelectedGroups()
    {
        try
        {
            if (!SelectedEmailGroupForTask.Any())
            {
                // If no groups are selected, return an empty collection or the full list, depending on your requirements
                return EmailDiaplysTable;
            }

            var filteredEmails = EmailAccounts
                ?.Where(email => SelectedEmailGroupForTask.Any(group => group.GroupId == email.Group.GroupId))
                .ToList();
            if (filteredEmails == null || !filteredEmails.Any())
            {
                // If no groups are selected, return an empty collection or the full list, depending on your requirements
                return EmailDiaplysTable;
            }
            return new ObservableCollection<EmailAccount>(filteredEmails!.ToObservableCollection());
        }
        catch (Exception e)
        {
            throw new Exception($"FilterEmailsBySelectedGroups failed: {e.Message}");
        }
        
    }

    #region campaign
    
    [RelayCommand]
    private void ReadAllEmailsInboxMessages()
    {
        _ = Task.Run(ReadAllEmailsInboxMessagesAsync);
    }

private async Task ReadAllEmailsInboxMessagesAsync()
{
    try
    {
        if (!SelectedProcesses.Any())
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Issue", "No process has been selected.");
            return;
        }

        var emailsGroupToWork = new List<EmailAccount>(FilterEmailsBySelectedGroups());
        var distributedBatches = ProxyListManager.DistributeEmailsBySubnetSingleList(emailsGroupToWork);

        if (!distributedBatches.Any())
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Issue", "The group selected contains no emails.");
            return;
        }

        if (SelectedProcesses[0] == null)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Selection Issue", "The process Selected has no corresponding Logic.");
            return;
        }
        
        foreach (var processName in SelectedProcesses)
        {
                var taskId = _taskManager.StartBatch(distributedBatches, async (emailAccount, cancellationToken) =>
                {
                    if (_processsMapping.TryGetValue(processName, out var processFunction))
                    {
                        var result = await processFunction(emailAccount);
                        if (processName == "CollectMessagesCount")
                        {
                            await PostCollectMessages(emailAccount.EmailAddress,
                                (ObservableCollection<FolderMessages>)result.ReturnedValue);
                        }
                        return result.Message;
                    }

                    return null;
                }, batchSize: ReportingSettingsValuesDisplay.Thread);

                await CreateAnActiveTask(TaskCategory.Active, TakInfoType.Batch, taskId, processName,
                    Statics.ReportingColor, Statics.ReportingSoftColor, distributedBatches.ToObservableCollection());
                await _taskManager.WaitForTaskCompletion(taskId); ;
        }
        await SaveMessagesToTextFile();
    }
    catch (Exception e)
    {
        ErrorIndicator = new ErrorIndicatorViewModel();
        await ErrorIndicator.ShowErrorIndecator("Process Issue", e.Message);
    }
}

    #region post processors

    #region collect data
  
        
        private readonly List<(string Email, List<string> Subjects)> _allMessagesList = new();
        private readonly object _allMessagesLock = new();

        private void StoreProcessedMessages(string email, IEnumerable<string> subjects)
        {
            lock (_allMessagesLock)
            {
                _allMessagesList.Add((email, subjects.ToList()));
            }
        }

        private async Task PostCollectMessages(string email, ObservableCollection<FolderMessages> inboxMessages)
        {
            var subjects = inboxMessages
                .Select(msg => msg.headers.subject)
                .Distinct()
                .ToList();

            StoreProcessedMessages(email, subjects);
        }

        private async Task SaveMessagesToTextFile()
        {
            try
            {

                if (IsGenerateNumberCollectionRequested)
                {
                    await GenerateNumberCollection();
                }

                if (IsGenerateCsvSubjectTableRequested)
                {

                    await GenerateSubjectCollection();

                }

            }
            catch (Exception ex)
            {
                ErrorIndicator = new ErrorIndicatorViewModel();
                await ErrorIndicator.ShowErrorIndecator("File saving", $"Failed to save messages to text file: {ex.Message}");
            }
            finally
            {
                // Clear the list after saving
                lock (_allMessagesLock)
                {
                    _allMessagesList.Clear();
                }
            }

            await Task.CompletedTask;
        }


        private async Task GenerateNumberCollection()
        {
            var filePath = Path.Combine(PathToSaveCountFile, $"{CountFileName}.txt");
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(fileStream))
            {
                writer.WriteLine("Email\tMessageCount");

                lock (_allMessagesLock)
                {
                    foreach (var (email, subjects) in _allMessagesList)
                    {
                        writer.WriteLine($"{email}\t{subjects.Count}");
                    }
                }
            }
                
        }

        private async Task GenerateSubjectCollection()
        {
            string filePath = Path.Combine(PathToSaveSubjectFile, $"{SubjectFileName}.txt");

            // Empty the file before writing new data
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(fileStream))
            {
                lock (_allMessagesLock)
                {
                    foreach (var entry in _allMessagesList)
                    {
                        var line = $"{entry.Email};{string.Join(";", entry.Subjects)}";
                        writer.WriteLine(line);
                    }
                }
            }
        }


    #endregion

    #endregion
    [RelayCommand]
    public async Task AddSingleCampaignTask()
    {
        _ = Task.Run(AddSingleCampaignTaskAsync);
        
       
    }

    private async Task AddSingleCampaignTaskAsync()
    {
        try
        {
        TimeSpan interval = TimeSpan.FromSeconds(ReportingSettingsValuesDisplay.RepetitionDelay);
        if (!SelectedProcesses.Any())
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Issue", "No process has been selected.");
            return;
        }
        var emailsGroupToWork = new List<EmailAccount>(FilterEmailsBySelectedGroups());
        //emailsGroupToWork.WriteListLine();
        var distributedBatches = ProxyListManager.DistributeEmailsBySubnetSingleList(emailsGroupToWork);
        //distributedBatches.WriteListLine("EmailBatches2");
        if (!distributedBatches.Any())
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
            }, ReportingSettingsValuesDisplay.Thread,ReportingSettingsValuesDisplay.Repetition,TaskCategory.Campaign,interval);
           
            await CreateAnActiveTask(TaskCategory.Campaign,TakInfoType.Batch,taskId,processName,Statics.UploadFileColor,Statics.UploadFileSoftColor,EmailAccounts);
            await _taskManager.WaitForTaskCompletion(taskId); ;
        }
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Campaign Issue", e.Message);
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
    
    
    
    #region Table and Filter Logic
    
    [ObservableProperty] private bool _isGridPopupOpen = false;
    [ObservableProperty] private bool _isGetContainSpam = false;
    [ObservableProperty] private ObservableCollection<Status> statuses;  
    [ObservableProperty] private ObservableCollection<ProxyStat> _byProxy;
    [ObservableProperty] private ObservableCollection<EmailAccount> _networkItems;
    [ObservableProperty] private ObservableCollection<EmailAccount> _emailDiaplysTable;
    [ObservableProperty] private ProxyStat _selectedByProxy;
    
    [ObservableProperty] private EmailGroup? _selectedEmailGroupFilter = null;
    [ObservableProperty] private string _searchLabel = "";
     [RelayCommand]
    private void ToggleGridFilterPopup()
    {
        IsGridPopupOpen = !IsGridPopupOpen;
    }
    
    
    
        [ObservableProperty]
        private bool _isBeforeChecked;
        [ObservableProperty]
        private int _countFilter;

        [ObservableProperty]
        private bool _isAfterChecked;
        [ObservableProperty]
        private DateTimeOffset? _selectedDate = DateTimeOffset.UtcNow; // Nullable to allow for no selection.

        [ObservableProperty]
        private TimeSpan? _selectedTime = DateTime.UtcNow.TimeOfDay; // Nullable to allow for no selection.
        [RelayCommand]
        private async Task FilterTable()
        {
            // Base query from original data
            var filteredList = NetworkItems.AsQueryable();

            // Filter by spam count only if IsGetContainSpam is true
            if (IsGetContainSpam)
            {
                filteredList = filteredList
                    .Where(item => item.Stats != null && item.Stats.SpamCount > 0);
            }

            // Filter by the selected email group
            if (SelectedEmailGroupFilter != null)
            {
                filteredList = filteredList
                    .Where(item => item.Group != null && item.Group.GroupId == SelectedEmailGroupFilter.GroupId);
            }

            // Update the display table
            EmailDiaplysTable = new ObservableCollection<EmailAccount>(filteredList);
            CountFilter = EmailDiaplysTable.Count;
        }

        [RelayCommand]
        private void CheckBefore()
        {
            IsBeforeChecked = true;
            IsAfterChecked = false;
        }

        [RelayCommand]
        private void CheckAfter()
        {
            IsBeforeChecked = false;
            IsAfterChecked = true;
        }
        [RelayCommand]
        private void ResetFilterTable()
        {
            
            EmailDiaplysTable = NetworkItems;
            CountFilter = EmailDiaplysTable.Count;
        }
    #endregion
   
}


