using DataAccess.Enums;
using Reporting.lib.Models.DTO;
using RepportingApp.CoreSystem.FileSystem;
using RepportingApp.CoreSystem.ProxyService;

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

    [ObservableProperty] private bool _isMenuOpen = true;
    [ObservableProperty] private bool _isPopupOpen;
    [ObservableProperty] private bool _isNotificationOpen;
    

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
    private Dictionary<string, Func<EmailAccount, Task<List<ReturnTypeObject>>>> _processsMapping;
    private readonly ProxyManagementPageViewModel _proxyManagementPageViewModel;
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
    [ObservableProperty] private string _emailAccountsTemporary ;

    #endregion
    

    #region mark messages as read & spam UI variables

    [ObservableProperty] private MarkMessagesAsReadConfig _markMessagesAsReadConfig = new MarkMessagesAsReadConfig();
    [ObservableProperty] private MarkMessagesAsReadConfig _markMessagesAsNotSpamConfig = new MarkMessagesAsReadConfig();
    [ObservableProperty] private MarkMessagesAsReadConfig _archiveMessagesConfig = new MarkMessagesAsReadConfig();
    #endregion
    #region Collect Directories Messages Variabless

        [ObservableProperty] 
        private List<string> _folderIds = new() { Statics.InboxDir };
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
        private ObservableCollection<KeyValuePair<string, string>> _selectedFolders = new();

        partial void OnSelectedFoldersChanged(ObservableCollection<KeyValuePair<string, string>> value)
        {
            FolderIds = value.Select(folder => folder.Value).ToList(); 
        }

    #endregion
    private readonly TaskInfoManager _taskInfoManager;
    private readonly ProxyListManager proxyListManager;
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
        ProxyManagementPageViewModel proxyManagementPageViewModel,
        IReportingRequests reportingRequests) : base(messenger)
    {
        proxyListManager = new ProxyListManager();
        _apiConnector = apiConnector;
        _reportingRequests = reportingRequests;
      
        _taskInfoManager = taskInfoManager;
        _configEstimator = configEstimator;
        _proxyManagementPageViewModel = proxyManagementPageViewModel;
        InitializeSettings();
        InitializeTaskManager();
        InitializeCommands();
        SubscribeToEvents();


        TasksCount = _taskInfoManager.GetTasksCount();
        //proxyListManager.UploadReservedProxyFile();
        

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
        _processsMapping = new Dictionary<string, Func<EmailAccount, Task<List<ReturnTypeObject>>>>
        {
            {"IsReportingSelected", async (emailAcc) =>
                {
                    if (emailAcc == null) throw new ArgumentNullException(nameof(emailAcc));
                    List<ReturnTypeObject> result = await _reportingRequests.ProcessMarkMessagesAsNotSpam(emailAcc,MarkMessagesAsNotSpamConfig);
                    return result; 
                }
            },
            {"MarkMessagesAsReadFromInbox",async (emailAcc) =>
                {
                    if (emailAcc == null) throw new ArgumentNullException(nameof(emailAcc));
                    return await _reportingRequests.ProcessMarkMessagesAsReadFromDirs(emailAcc,MarkMessagesAsReadConfig,FolderIds);
                }
            },
            {"CollectMessagesCount",async (emailAcc) =>
                {
                    if (emailAcc == null) throw new ArgumentNullException(nameof(emailAcc));
                    return await _reportingRequests.ProcessGetMessagesFromDirs(emailAcc,
                        FolderIds);
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
        // Fetch API data in parallel, awaiting them properly
        var fetchGroupsTask = _apiConnector.GetDataAsync<IEnumerable<EmailGroup>>(ApiEndPoints.GetGroups, ignoreCache:ignoreCache);
        var fetchEmailsTask = _apiConnector.GetDataAsync<IEnumerable<EmailAccount>>(ApiEndPoints.GetEmails, ignoreCache:ignoreCache);
        var fetchProxiesTask = _apiConnector.GetDataAsync<IEnumerable<Proxy>>(ApiEndPoints.GetAllProxies, ignoreCache:ignoreCache);

        // Await tasks separately
        var groups = await fetchGroupsTask;
        var emails = await fetchEmailsTask;
        var proxies = await fetchProxiesTask;

        
        Task.Run(() => proxyListManager.GetDBProxies(proxies));

        // Efficiently update observable collections
        Groups = groups.ToObservableCollection();
        NetworkItems = emails.ToObservableCollection();
        EmailDiaplysTable = new ObservableCollection<EmailAccount>(NetworkItems);
        EmailAccounts = emails.ToObservableCollection();
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
    [ObservableProperty] private string _rdpIp;
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
        try
        {
            IsEnabled = false;
            if (string.IsNullOrWhiteSpace(NewGroupName) || string.IsNullOrWhiteSpace(RdpIp))
            {
                throw new Exception("You must enter a valid RDP IP address and group name  for new group");
            }

            var chunkPayload = new EmailGroup()
                {
                    GroupName = NewGroupName,
                    RdpIp = RdpIp,
                };
                var id = await _apiConnector.PostDataObjectAsync<int>(ApiEndPoints.PostGroup, chunkPayload);
                EmailGroup newGroup = new EmailGroup()
                {
                    GroupId = id,
                    GroupName = NewGroupName
                };
                Groups.Add(newGroup);
                IsEnabled = true;
                SelectedEmailGroup = newGroup;

                NewGroupName = string.Empty;
                RdpIp = string.Empty;

     
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Group Creation Failed",
                e.Message);
        }
        finally
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
            await ErrorIndicator.ShowErrorIndecator("Invalid File", "You dropped an invalid file extension (only txt, Excel format)");
            return;
        }

        if (SelectedEmailGroup?.GroupName == null)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("No group selected", "You have to assign a group or provide a name for a new group.");
            return;
        }

        var fileInfo = new FileInfo(filePath);
        FileName = fileInfo.Name;
        FileSize = Math.Floor(fileInfo.Length / (1024.0 * 1024.0)); // Convert to MB

        _cancellationTokenSource = new CancellationTokenSource();
        IsUploading = true;
        UploadProgress = 0;
        _processedLines = 0;

        var fileLines = File.ReadLines(filePath).ToList();
        var totalLines = fileLines.Count;
        var progress = new Progress<int>(value => UploadProgress = value);

        // Process Email Accounts in Batches
        var batches = fileLines.Batch(1000);
        foreach (var batch in batches)
        {
            var currentBatchId = _taskManager.StartBatch(
                batch,
                (line, cancellationToken) => ProcessLineWithProgress(line, totalLines, progress, cancellationToken),
                TaskCategory.Invincible,
                batchSize: _configEstimator.RecommendedBatchSize
            );

            await CreateAnActiveTask(TaskCategory.Active, TaskCategory.Invincible, TakInfoType.Batch, currentBatchId, "Profile Upload", Statics.UploadFileColor, Statics.UploadFileSoftColor, null);
            await _taskManager.WaitForTaskCompletion(currentBatchId);
        }

        // Process Metadata (Optional)
        var metadataFilePath = FileManager.GetMasterIdsPath();
        if (File.Exists(metadataFilePath))
        {
            var metadataLines = File.ReadLines(metadataFilePath).ToList();
            var metadataBatches = metadataLines.Batch(1000);

            foreach (var batch in metadataBatches)
            {
                var currentTaskId = _taskManager.StartBatch(
                    batch,
                    async (line, cancellationToken) => await ProcessMetaData(line, cancellationToken),
                    TaskCategory.Invincible,
                    batchSize: _configEstimator.RecommendedBatchSize
                );

                await CreateAnActiveTask(TaskCategory.Active, TaskCategory.Invincible, TakInfoType.Batch, currentTaskId, "Metadata Upload", Statics.UploadFileColor, Statics.UploadFileSoftColor, null);
                await _taskManager.WaitForTaskCompletion(currentTaskId);
            }
        }

        // Prepare Data for API
        int? groupId = SelectedEmailGroup.GroupId;
        string? groupName = SelectedEmailGroup.GroupName;

        var emailsSnapshot = _emailsToGetUploaded.ToList();
        var metadataSnapshot = _metadataDictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Handle Missing Metadata Gracefully
        var unmatchedEmails = emailsSnapshot
            .Where(email => !metadataSnapshot.ContainsKey(email.EmailAddress))
            .ToList();

        if (unmatchedEmails.Any())
        {
            Console.WriteLine($"Warning: {unmatchedEmails.Count} emails have no metadata. They will be uploaded without metadata.");
        }

        // Process in Chunks
        const int chunkSize = 1000;
        var chunks = emailsSnapshot
            .Select((email, index) => new { email, index })
            .GroupBy(x => x.index / chunkSize)
            .Select(group => new
            {
                Emails = group.Select(x => x.email).ToList(),
                Metadata = group.Select(x => x.email.EmailAddress)
                    .Where(email => metadataSnapshot.ContainsKey(email))
                    .ToDictionary(email => email, email => metadataSnapshot[email])
            })
            .ToList();

        foreach (var chunk in chunks)
        {
            var chunkPayload = new
            {
                emailAccounts = chunk.Emails,
                emailMetadata = chunk.Metadata, // Only matching metadata
                groupId,
                groupName
            };

            var apiCreateEmails = _taskManager.StartTask(async cancellationToken =>
            {
                await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.GetAddEmails, chunkPayload);
            });

            await CreateAnActiveTask(TaskCategory.Invincible, TaskCategory.Invincible, TakInfoType.Single, apiCreateEmails,
                "Create Emails (API)", Statics.UploadFileColor, Statics.UploadFileSoftColor, null);

            await _taskManager.WaitForTaskCompletion(apiCreateEmails);
        }

        await ReconnectToApi();
    }
    catch (Exception e)
    {
        ErrorIndicator = new ErrorIndicatorViewModel();
        await ErrorIndicator.ShowErrorIndecator("File Upload Error", e.Message);
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
        if (data.Length < 3)
        {
            // Invalid format, log or skip
            return "";
        }

        var emailAccount = new CreateEmailAccountDto
        {
            EmailAddress = data[0],
            Password = data[1],
            RecoveryEmail = data[2],
            Status = EmailStatus.NewAdded,
            Group = SelectedEmailGroup,
        };

        // Handle optional proxy (Ensure at least 7 elements exist)
        if (data.Length >= 7 && !string.IsNullOrWhiteSpace(data[3]))
        {
            emailAccount.Proxy = new ProxyDto
            {
                ProxyIp = data[3],
                Port = int.TryParse(data[4], out var port) ? port : 0,
                Username = data[5],
                Password = data[6],
                Availability = false,
                YahooConnectivity = "",
                Region = ""
            };
        }

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
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(line))
                return "";

            var data = line.Split(';');
            if (data.Length < 4)
                return "";

            var email = data[0];
            var metadataDto = new EmailMetadataDto
            {
                EmailAddress = data[0],
                MailId = data[1],
                YmreqId = data[2],
                Wssid = data[3],
                Cookie = null // Default to null, update below if file exists
            };

            var cookiePath = Path.Combine(FileManager.GetProfileCookieFolder(email + ".txt"));
            if (File.Exists(cookiePath))
            {
                metadataDto.Cookie = await File.ReadAllTextAsync(cookiePath, cancellationToken);
            }

            _metadataDictionary.TryAdd(email, metadataDto);
            return "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ProcessMetaData: {ex.Message}");
            return "";
        }
    }

    private bool ValidateFile(string filePath)
    {
        var validExtensions = new[] { ".txt" };
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
    
    public async Task  CreateAnActiveTask(TaskCategory startcategory, TaskCategory moveToCategory, TakInfoType type, Guid taskId, string name, string color, string softColor,ObservableCollection<EmailAccount>? assignedGroupEmails)
    {
        await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
        {
            var taskInfo = new TaskInfoUiModel(type,startcategory,moveToCategory)
            {
                StartTime = DateTime.UtcNow.ToString("t"),
                TaskId = taskId,
                Name = name,
                Color = color,
                SoftColor = softColor,
                AssignedEmails = assignedGroupEmails != null ? new ObservableCollection<EmailAccount>(assignedGroupEmails) : new ObservableCollection<EmailAccount>(),
                AssignedEmailsDisplayInfo =  assignedGroupEmails != null ? new ObservableCollection<EmailAccount>(assignedGroupEmails) : new ObservableCollection<EmailAccount>(),
                CancelCommand = new RelayCommand(() =>
                {
                    CancelTask(taskId);
                    _taskInfoManager.CompleteTask(taskId,startcategory,moveToCategory);
                    TasksCount = _taskInfoManager.GetTasksCount();
                })
            };
            var assignedGroup = taskInfo.CheckCommonGroupName(assignedGroupEmails);
            taskInfo.AssignedGroup = assignedGroup;
            _taskInfoManager.AddTask(startcategory, taskInfo);
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
        TaskInfoUiModel? taskInfo = _taskInfoManager.GetTaskById(e.TaskId);
        TaskMessages.Add($"Task {e.TaskId} completed successfully.");
        _taskInfoManager.CompleteTask(e.TaskId,taskInfo.StarterCategory,taskInfo.MovetoCategory);
        TasksCount = _taskInfoManager.GetTasksCount();
        
    }
    
    private void OnTaskErrored(object? sender, TaskErrorEventArgs e)
    {

        TaskInfoUiModel? taskInfo = _taskInfoManager.GetTaskById(e.TaskId);

        ErrorMessages.Add($"Task {e.TaskId} encountered an error: {e.Error.Message}");
        _taskInfoManager.CompleteTask(e.TaskId,taskInfo.StarterCategory,taskInfo.MovetoCategory);
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
                      
                        /*await Dispatcher.UIThread.InvokeAsync(
                            () =>
                            {
                                EmailDiaplysTable = NetworkItems;
                
                            });*/
                    });
                    
                    await CreateAnActiveTask(TaskCategory.Active,TaskCategory.Invincible,TakInfoType.Single,apiCreateEmails,"update api ",Statics.ReportingColor,Statics.ReportingSoftColor,null);
                    TaskMessages.Add($"Email stats updated for {processedEmails.Count} emails in task {e.TaskId}.");
                }
                catch (Exception ex)
                {
                    ErrorMessages.Add($"Failed to update email stats for task {e.TaskId}: {ex.Message}");
                }
            }

            try
            {
                if (failedEmailsIds.Any())
                {
                    await _apiConnector.PostDataObjectAsync<object>(
                        ApiEndPoints.AddEmailsToFail, 
                        failedEmailsIds 
                    );
                    failedEmailsIds.Clear();
                }
            }
            catch (Exception exception)
            {
            }
        }
    }

private ConcurrentBag<FailedEmailDto> failedEmailsIds = new ConcurrentBag<FailedEmailDto>();
private void OnItemProcessed(object? sender, ItemProcessedEventArgs e)
{
    try
    {
        var taskCollectionsToSearch = new[]
        {
            _taskInfoManager.GetTasks(TaskCategory.Active),
            _taskInfoManager.GetTasks(TaskCategory.Campaign),
            _taskInfoManager.GetTasks(TaskCategory.Notification),
        };

        TaskInfoUiModel? taskInfo = taskCollectionsToSearch
            .SelectMany(tasks => tasks)
            .FirstOrDefault(t => t.TaskId == e.TaskId);

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
                        if (e.Error != null && !e.Error.Message.Contains("Object reference not")||
                            !errorMessage.Contains("A connection attempt failed because the connected")||
                            !errorMessage.Contains("Unexpected error: The request was canceled due to the configured HttpClient."))
                        {
                            failedEmailsIds.Add(new FailedEmailDto()
                            {
                                EmailId = emailProcessed.Id,
                                FailureReason = $"{e.Error?.Message}"
                            });
                        }
                       
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
            // Check if the TextBox contains emails
            if (!string.IsNullOrWhiteSpace(EmailAccountsTemporary))
            {
                var enteredEmails = EmailAccountsTemporary
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) // Split by newline
                    .Select(e => e.Trim())
                    .ToHashSet(); // Convert to HashSet for fast lookup

                var tempraryG = EmailAccounts.Where(email => enteredEmails.Contains(email.EmailAddress)).ToList();
                return tempraryG.ToObservableCollection();
            }
            
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

    private async Task LoadEmailsFromDb()
    { 
        await _proxyManagementPageViewModel.AssignProxiesCommand.ExecuteAsync(null);
        var fetchEmailsTask = _apiConnector.GetDataAsync<IEnumerable<EmailAccount>>(ApiEndPoints.GetEmails, ignoreCache:true);
        var emails = await fetchEmailsTask;
        NetworkItems = emails.ToObservableCollection();
        EmailDiaplysTable = new ObservableCollection<EmailAccount>(NetworkItems);
        EmailAccounts = new ObservableCollection<EmailAccount>(NetworkItems);
    }
private async Task ReadAllEmailsInboxMessagesAsync()
{
    try
    {
        // assign proxies to email
        await LoadEmailsFromDb();
        if (!SelectedProcesses.Any())
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Issue", "No process has been selected.");
            return;
        }

        var emailsGroupToWork = new List<EmailAccount>(FilterEmailsBySelectedGroups());
        var distributedBatches = proxyListManager.DistributeEmailsBySubnetSingleList(emailsGroupToWork);

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
                        var results = await processFunction(emailAccount);
                        foreach (var result in results)
                        {
                            if (processName == "CollectMessagesCount")
                            {
                                await PostCollectMessages(
                                    emailAccount.EmailAddress,
                                    (ObservableCollection<FolderMessages>)result.ReturnedValue
                                );
                            }
                        }
                        return string.Join("\n", results.Select(r => r.Message));

                    }

                    return null;
                }, batchSize: ReportingSettingsValuesDisplay.Thread);

                await CreateAnActiveTask(TaskCategory.Active,TaskCategory.Saved, TakInfoType.Batch, taskId, processName,
                    Statics.ReportingColor, Statics.ReportingSoftColor, distributedBatches.ToObservableCollection());
                await _taskManager.WaitForTaskCompletion(taskId);
                await LoadEmailsFromDb();
        }
        await SaveMessagesToTextFile();
        await LoadDataAsync(true);
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
    [ObservableProperty]
    private TimeSpan _timeUntilNextRun; 
    [ObservableProperty]
    private string _state; 
    [ObservableProperty]
    private int _tour;   
    [ObservableProperty]
    private int _Totalrepitition;  
    [ObservableProperty]
    private bool isRunning;

    private async Task AddSingleCampaignTaskAsync()
    {
        try
        {
            await LoadEmailsFromDb();

            Totalrepitition = ReportingSettingsValuesDisplay.Repetition;
            TimeSpan interval = TimeSpan.FromSeconds(ReportingSettingsValuesDisplay.RepetitionDelay);
            if (!SelectedProcesses.Any())
            {
                ErrorIndicator = new ErrorIndicatorViewModel();
                await ErrorIndicator.ShowErrorIndecator("Process Issue", "No process has been selected.");
                return;
            }

            var emailsGroupToWork = new List<EmailAccount>(FilterEmailsBySelectedGroups());
            //emailsGroupToWork.WriteListLine();
            var distributedBatches = proxyListManager.DistributeEmailsBySubnetSingleList(emailsGroupToWork);
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
                await ErrorIndicator.ShowErrorIndecator("Process Selection Issue",
                    "The process Selected have no correspondent Logic.");
                return;
            }

            var results = new List<ReturnTypeObject>() { new ReturnTypeObject() { Message = "Nothing inside" } };
            Tour = 0;
            while (Tour < ReportingSettingsValuesDisplay.Repetition)
            {
                IsRunning = true;
                State = "Running";
                foreach (var processName in SelectedProcesses)
                {
                    var taskId = _taskManager.StartLoopingTaskBatch(distributedBatches,
                        async (emailAccount, cancellationToken) =>
                        {
                            if (_processsMapping.TryGetValue(processName, out var processFunction))
                            {
                                results = await processFunction(emailAccount);
                                foreach (var result in results)
                                {
                                    if (processName == "CollectMessagesCount")
                                    {
                                        await PostCollectMessages(
                                            emailAccount.EmailAddress,
                                            (ObservableCollection<FolderMessages>)result.ReturnedValue
                                        );
                                    }
                                }

                                return string.Join("\n", results.Select(r => r.Message));
                            }

                            return string.Join("\n", results.Select(r => r.Message));
                        }, ReportingSettingsValuesDisplay.Thread, TaskCategory.Campaign, interval);

                    await CreateAnActiveTask(TaskCategory.Campaign, TaskCategory.Saved, TakInfoType.Batch, taskId,
                        processName, Statics.UploadFileColor, Statics.UploadFileSoftColor,
                        distributedBatches.ToObservableCollection());
                    await _taskManager.WaitForTaskCompletion(taskId);
                    await LoadEmailsFromDb();
                }
            
                Tour++;
                State = "Waiting";
                DateTime nextRunTime = DateTime.UtcNow + interval;

                while (DateTime.UtcNow < nextRunTime)
                {
                    var remainingTime = nextRunTime - DateTime.UtcNow;

                    TimeUntilNextRun = remainingTime;

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

            }

            await LoadDataAsync(true);
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Campaign Issue", e.Message);
        }
        finally
        {
            Tour = 0;
            IsRunning = false;
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
    [ObservableProperty] private int _spamCountMin = 0;
    [ObservableProperty] private int _spamCountMax = 20;
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

            // Apply spam filtering only if IsGetContainSpam is true
            if (IsGetContainSpam)
            {
                filteredList = filteredList
                    .Where(item => item.Stats != null && item.Stats.SpamCount > 0);

                // Apply spam count range filter only if spam filtering is enabled
                filteredList = filteredList
                    .Where(item => item.Stats != null &&
                                   item.Stats.SpamCount >= SpamCountMin &&
                                   item.Stats.SpamCount <= SpamCountMax);
            }

            // Filter by the selected email group
            if (SelectedEmailGroupFilter != null)
            {
                bool isGroupEmpty = !NetworkItems.Any(item => 
                    item.Group != null && 
                    item.Group.GroupId == SelectedEmailGroupFilter.GroupId);

                if (!isGroupEmpty)
                {
                    filteredList = filteredList
                        .Where(item => item.Group != null && item.Group.GroupId == SelectedEmailGroupFilter.GroupId);
                }
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


