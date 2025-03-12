using Reporting.lib.Models.DTO;
using RepportingApp.CoreSystem.ProxyService;
using RepportingApp.Request_Connection_Core.MailBox;

namespace RepportingApp.ViewModels;

public partial class MailBoxPageViewModel : ViewModelBase,ILoadableViewModel
{
    [ObservableProperty] private ReportingSettingValues _reportingSettingsValuesDisplay;
    [ObservableProperty] private string _customName= "Some Name";
    [ObservableProperty] private int _count = 1;
    [ObservableProperty] public ErrorIndicatorViewModel _errorIndicator= new ErrorIndicatorViewModel();
    [ObservableProperty] private bool _isDataLoaded = false;
    [ObservableProperty] private bool _isLoading;
    private readonly TaskInfoManager _taskInfoManager;
    private readonly ProxyListManager proxyListManager;
    private readonly IApiConnector _apiConnector;
    private readonly IMailBoxRequests _mailBoxRequests;
    [ObservableProperty] private int _tasksCount;
    private UnifiedTaskManager _taskManager;
    public MailBoxPageViewModel(IMessenger messenger  ,IApiConnector apiConnector,   TaskInfoManager taskInfoManager,
        IMailBoxRequests mailBoxRequests) : base(messenger)
    {
        _taskInfoManager = taskInfoManager;
        _apiConnector = apiConnector;
        _mailBoxRequests = mailBoxRequests;
        proxyListManager = new ProxyListManager();
        InitializeTaskManager();
        InitializeSettings();
        SubscribeToEvents();
        TasksCount = _taskInfoManager.GetTasksCount();
    }
    private void SubscribeToEvents()
    {
        SubscribeEventToThread();
    }
    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        await ReportingSettingsValuesDisplay.SaveConfigurationAsync();
    }
    
    private void InitializeSettings()
    {
        ReportingSettingsValuesDisplay = new ReportingSettingValues(App.Configuration);
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
    private void InitializeTaskManager()
    {
        var systemEstimator = new SystemConfigurationEstimator();
        _taskManager = new UnifiedTaskManager(systemEstimator.RecommendedMaxDegreeOfParallelism, _taskInfoManager);
    }
    [ObservableProperty] private ObservableCollection<EmailGroup> _groups= new ObservableCollection<EmailGroup>();
    [ObservableProperty] private ObservableCollection<EmailAccount> _networkItems;
    [ObservableProperty] private ObservableCollection<EmailAccount> _emailDiaplysTable;
    [ObservableProperty] private ObservableCollection<EmailAccount>? _emailAccounts = new();
    [ObservableProperty] private bool _isGroupSettingsDropdownOpen1 = false;
    [RelayCommand] private void ToggleGroupSelctionMenuOne() => IsGroupSettingsDropdownOpen1 = !IsGroupSettingsDropdownOpen1; 
    [ObservableProperty] private ObservableCollection<EmailGroup> _selectedEmailGroupForTask = new (); 
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
    }
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
    [RelayCommand]
    private void InitializeMailBox()
    {
        _ = Task.Run(InitializeMailBoxAync);
        
    }

    #region Initialize sub methods

        private async Task CollectMailboxesAsync(List<EmailAccount> distributedBatches)
    {
        ConcurrentBag<MailBoxDto> mailBoxDtos = new();
        var taskId2 = _taskManager.StartBatch(distributedBatches, async (emailAccount, cancellationToken) =>
        {
            if (emailAccount == null) throw new ArgumentNullException(nameof(emailAccount));
            ReturnTypeObject result = await _mailBoxRequests.ProcessCollectAlias(emailAccount);
            foreach (var mailBox in (List<MailBoxDto>)result.ReturnedValue)
            {
                mailBoxDtos.Add(mailBox);
            }
            return null;
        }, batchSize: ReportingSettingsValuesDisplay.MailBoxThread);

        await CreateAnActiveTask(TaskCategory.Active,TaskCategory.Saved, TakInfoType.Batch, taskId2, "Initialize MailBox",
            Statics.ReportingColor, Statics.ReportingSoftColor, distributedBatches.ToObservableCollection());
        await _taskManager.WaitForTaskCompletion(taskId2);
            
        var apiCreateEmails2 = _taskManager.StartTask(async cancellationToken =>
        {
            await _apiConnector.PostDataObjectAsync<string>(
                ApiEndPoints.MailboxesAdd,
                mailBoxDtos);
        });

        await CreateAnActiveTask(TaskCategory.Active, TaskCategory.Invincible, TakInfoType.Single, apiCreateEmails2,
            "update api ", Statics.ReportingColor, Statics.ReportingSoftColor, null);
        await _taskManager.WaitForTaskCompletion(apiCreateEmails2);
    }
    private async Task GetNickNameAsync(List<EmailAccount> distributedBatches)
    {
        ConcurrentBag<NetworkLogDto> networkLogEntries = new();
        var taskId = _taskManager.StartBatch(distributedBatches, async (emailAccount, cancellationToken) =>
        {
            if (emailAccount == null) throw new ArgumentNullException(nameof(emailAccount));
            ReturnTypeObject result = await _mailBoxRequests.PrepareMailBoxNickName(emailAccount);
            Console.WriteLine(result);
            NetworkLogDto entry = new NetworkLogDto()
            {
                EmailId = emailAccount.Id,
                NickName = (string)result.ReturnedValue
            };
            networkLogEntries.Add(entry);
            return "Sucessfully Retrieved Network Log";
        }, batchSize: ReportingSettingsValuesDisplay.MailBoxThread);

        await CreateAnActiveTask(TaskCategory.Active,TaskCategory.Saved, TakInfoType.Batch, taskId, "Initialize MailBox",
            Statics.ReportingColor, Statics.ReportingSoftColor, distributedBatches.ToObservableCollection());
        await _taskManager.WaitForTaskCompletion(taskId);
        var apiCreateEmails = _taskManager.StartTask(async cancellationToken =>
        {
            await _apiConnector.PostDataObjectAsync<string>(
                ApiEndPoints.NetworkLogAdd,
                networkLogEntries);
        });

        await CreateAnActiveTask(TaskCategory.Active, TaskCategory.Invincible, TakInfoType.Single, apiCreateEmails,
            "update api ", Statics.ReportingColor, Statics.ReportingSoftColor, null);
        await _taskManager.WaitForTaskCompletion(apiCreateEmails);
    }
    private async Task CreateAliasesAsync(List<EmailAccount> distributedBatches,List<NetworkLogDto> networkLogs)
    {
        ConcurrentBag<NetworkLogDto> networkLogEntries = new();
        var taskId = _taskManager.StartBatch(distributedBatches, async (emailAccount, cancellationToken) =>
        {
            if (emailAccount == null) throw new ArgumentNullException(nameof(emailAccount));
            var networkLog = networkLogs.FirstOrDefault(n => n.EmailId == emailAccount.Id);
            if (networkLog == null) throw new NullReferenceException("Nickname is null");
            if (networkLog.MailboxesCount >= 3) return "this mailbox already have 3 aliases";
            ReturnTypeObject result = await _mailBoxRequests.CreateAliaseManagerInitializer(emailAccount,Count,networkLog.NickName,CustomName);
            Console.WriteLine(result);
            return "Sucessfully Created Alias Manager";
        }, batchSize: ReportingSettingsValuesDisplay.MailBoxThread);

        await CreateAnActiveTask(TaskCategory.Active,TaskCategory.Saved, TakInfoType.Batch, taskId, "Create MailBox",
            Statics.ReportingColor, Statics.ReportingSoftColor, distributedBatches.ToObservableCollection());
        await _taskManager.WaitForTaskCompletion(taskId);
        /*var apiCreateEmails = _taskManager.StartTask(async cancellationToken =>
        {
            await _apiConnector.PostDataObjectAsync<object>(
                ApiEndPoints.NetworkLogAdd,
                networkLogEntries);
        });*/

        /*await CreateAnActiveTask(TaskCategory.Active,TaskCategory.Invincible,TakInfoType.Single,apiCreateEmails,"update api ",Statics.ReportingColor,Statics.ReportingSoftColor,null);*/
    }

    #endregion

    private async Task InitializeMailBoxAync()
    {
        try
        {
            await LoadEmailsFromDb();

            var emailsGroupToWork = new List<EmailAccount>(FilterEmailsBySelectedGroups());
            var distributedBatches = proxyListManager.DistributeEmailsBySubnetSingleList(emailsGroupToWork);

            if (!distributedBatches.Any())
            {
                ErrorIndicator = new ErrorIndicatorViewModel();
                await ErrorIndicator.ShowErrorIndecator("Process Issue", "The group selected contains no emails.");
                return;
            }
            
            await CollectMailboxesAsync(distributedBatches);
            await GetNickNameAsync(distributedBatches);
            var networkLogs = await _apiConnector.GetDataAsync<IEnumerable<NetworkLogDto>>(ApiEndPoints.GetAllMailBoxes, ignoreCache:false);
            await CreateAliasesAsync(distributedBatches,networkLogs.ToList());
            await CollectMailboxesAsync(distributedBatches);
            await LoadDataAsync(true);
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Issue", e.Message);
        }
    }
    private async Task LoadEmailsFromDb()
    {
        var fetchEmailsTask = _apiConnector.GetDataAsync<IEnumerable<EmailAccount>>(ApiEndPoints.GetEmails, ignoreCache:true);
        var emails = await fetchEmailsTask;
        NetworkItems = emails.ToObservableCollection();
        EmailDiaplysTable = new ObservableCollection<EmailAccount>(NetworkItems);
        EmailAccounts = emails.ToObservableCollection();
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
    [RelayCommand]
    public void CancelTask(Guid taskId)
    {
        _taskManager.CancelTask(taskId);
    }
        #region events and subscription

    private void SubscribeEventToThread()
    {
        _taskManager.TaskCompleted += OnTaskCompleted;
        _taskManager.TaskErrored += OnTaskErrored;
        _taskManager.BatchCompleted += OnBatchCompleted;
        _taskManager.ItemProcessed += OnItemProcessed;
        
    }

    [ObservableProperty] private string _taskMessages;
    [ObservableProperty] private string _errorMessages;
    private void OnTaskCompleted(object? sender, TaskCompletedEventArgs e)
    {
        TaskInfoUiModel? taskInfo = _taskInfoManager.GetTaskById(e.TaskId);
        TaskMessages = $"Task {e.TaskId} completed successfully.";
        _taskInfoManager.CompleteTask(e.TaskId,taskInfo.StarterCategory,taskInfo.MovetoCategory);
        TasksCount = _taskInfoManager.GetTasksCount();
        
    }
    
    private void OnTaskErrored(object? sender, TaskErrorEventArgs e)
    {

        TaskInfoUiModel? taskInfo = _taskInfoManager.GetTaskById(e.TaskId);

        ErrorMessages = $"Task {e.TaskId} encountered an error: {e.Error.Message}";
        _taskInfoManager.CompleteTask(e.TaskId,taskInfo.StarterCategory,taskInfo.MovetoCategory);
        ErrorIndicator = new ErrorIndicatorViewModel();
        ErrorIndicator.ShowErrorIndecator("Invalid File", e.Error.Message);
        TasksCount = _taskInfoManager.GetTasksCount();
    }

    private async void OnBatchCompleted(object? sender, BatchCompletedEventArgs e)
    {
        
        /*TaskMessages.Add($"Batch task {e.TaskId} completed.");*/
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
                    /*TaskMessages.Add($"Email stats updated for {processedEmails.Count} emails in task {e.TaskId}.");*/
                }
                catch (Exception ex)
                {
                    ErrorMessages = $"Failed to update email stats for task {e.TaskId}: {ex.Message}";
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
                        TaskMessages = ($"Email processed successfully in task {e.TaskId}.");
                    }
                    else
                    {
                        taskInfo.ItemSuccesMessasges.Add(new ItemInfo()
                        {
                            
                            Message = $"Non-email item processed successfully in task {e.TaskId}: {e.Item}"
                        });
                        TaskMessages = ($"Non-email item processed successfully in task {e.TaskId}: {e.Item}");
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
                        if (e.Error != null && !e.Error.Message.Contains("proxy"))
                        {
                            failedEmailsIds.Add(new FailedEmailDto()
                            {
                                EmailId = emailProcessed.Id,
                                FailureReason = $"{e.Error?.Message}"
                            });
                        }
                       
                        ErrorMessages = ($"Email failed in task {e.TaskId}: {e.Error?.Message}");
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
                        ErrorMessages = ($"Non-email item failed in task {e.TaskId}: {e.Error?.Message}");
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
}