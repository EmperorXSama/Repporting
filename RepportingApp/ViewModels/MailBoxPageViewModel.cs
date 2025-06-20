using Reporting.lib.Models.DTO;
using RepportingApp.CoreSystem.ProxyService;
using RepportingApp.Request_Connection_Core.MailBox;

namespace RepportingApp.ViewModels;

public partial class MailBoxPageViewModel : ViewModelBase,ILoadableViewModel
{
    [ObservableProperty] private ReportingSettingValues _reportingSettingsValuesDisplay;
    [ObservableProperty] private string _customName= "Some Name";
    [ObservableProperty] private string _successMessage;
    [ObservableProperty] private string _errorMessage;
    [ObservableProperty] private int _count = 3;
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
        FilterSetupInitializer();
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


    public bool IsLoadNext { get; set; }

    public async Task LoadDataIfFirstVisitAsync(bool ignorecache = false)
    {
        try
        {
            if (!IsLoading && !IsLoadNext )
            {
                IsLoading = true;
                // Load data here
                await LoadDataAsync(ignorecache);
                IsLoadNext = true;
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

    private void FilterSetupInitializer()
    {
        SelectedEmailGroupForTask.CollectionChanged += OnSelectedGroupsChanged;
    }
    
    private void InitializeTaskManager()
    {
        var systemEstimator = new SystemConfigurationEstimator();
        _taskManager = new UnifiedTaskManager(systemEstimator.RecommendedMaxDegreeOfParallelism, _taskInfoManager);
    }
    [ObservableProperty] private ObservableCollection<EmailGroup> _groups= new ObservableCollection<EmailGroup>();
    [ObservableProperty] private ObservableCollection<EmailAccount> _networkItems;
    [ObservableProperty] private ObservableCollection<EmailAccount> _emailDiaplysTable;
    [ObservableProperty] private ObservableCollection<EmailMailboxDetails> _emailsMailboxesDetails;
    [ObservableProperty] private ObservableCollection<EmailMailboxDetails> _emailsMailboxesDetailsTable;


    [ObservableProperty] private ObservableCollection<EmailAccount>? _emailAccounts = new();
    [ObservableProperty] private bool _isGroupSettingsDropdownOpen1 = false;
    [RelayCommand] private void ToggleGroupSelctionMenuOne() => IsGroupSettingsDropdownOpen1 = !IsGroupSettingsDropdownOpen1; 
    
    private async Task LoadDataAsync(bool ignoreCache)
    {
        IsDataLoaded = true;
        // Fetch API data in parallel, awaiting them properly
        var fetchGroupsTask =
            _apiConnector.GetDataAsync<IEnumerable<EmailGroup>>(ApiEndPoints.GetGroups, ignoreCache: ignoreCache);
 
        var fetchEmailsTask = _apiConnector.GetDataAsync<IEnumerable<EmailAccount>>(ApiEndPoints.GetEmails, ignoreCache:ignoreCache);
        var fetchProxiesTask = _apiConnector.GetDataAsync<IEnumerable<Proxy>>(ApiEndPoints.GetAllProxies, ignoreCache:ignoreCache);

        // Await tasks separately
        var groups = await fetchGroupsTask;
        var emails = await fetchEmailsTask;
       
        var proxies = await fetchProxiesTask;

        
        Task.Run(() => proxyListManager.GetDBProxies(proxies));
        await LoadEmailDetailsAsync();
        // Efficiently update observable collections
        Groups = groups.ToObservableCollection();
        NetworkItems = emails.ToObservableCollection();
        
      
        EmailDiaplysTable = new ObservableCollection<EmailAccount>(NetworkItems);
        var emailToGroupMap = EmailDiaplysTable
            .Where(e => e.Group != null)
            .ToDictionary(e => e.EmailAddress, e => e.Group.GroupName);

        // ✅ Use dictionary lookup instead of LINQ query inside loop
        foreach (var detail in EmailsMailboxesDetailsTable)
        {
            if (emailToGroupMap.TryGetValue(detail.EmailAddress, out var groupName))
            {
                detail.GroupName = groupName;
            }
        }
        EmailAccounts = emails.ToObservableCollection();
        IsDataLoaded = false;
    }

    [RelayCommand]
    private async Task ResetData()
    {
        await LoadDataAsync(false);
    }
 
    private async Task LoadEmailDetailsAsync()
    {
        var mailboxesDetails = _apiConnector.GetDataAsync<IEnumerable<EmailMailboxDetails>>(ApiEndPoints.GetAllEmailsWithMailboxes, ignoreCache:false);
        var emailsDetails = await mailboxesDetails;
        EmailsMailboxesDetails = emailsDetails.ToObservableCollection();
        EmailsMailboxesDetailsTable = new ObservableCollection<EmailMailboxDetails>(
            EmailsMailboxesDetails
                .OrderByDescending(e => e.MailboxesCount ?? 0) 
        );
        CountFilter = EmailsMailboxesDetailsTable.Count;
        MaxTotalPacks = EmailsMailboxesDetailsTable.Max(e => e.TotalPacks);
        // ✅ Create dictionary for fast lookups
        
    }
    
    [RelayCommand]
    private void InitializeMailBox()
    {
        _ = Task.Run(InitializeMailBoxAync);
        
    } 
    [RelayCommand]
    private void RefreachAndClean()
    {
        _ = Task.Run(RefreachAndCleanMailBoxAync);
        
    }

    #region delete aliases
    [ObservableProperty] private bool _isdeleteExtra = false;

    private async Task<List<MailBoxDto>> GetAliasesAsyncWithoutApiUpdate(List<EmailAccount> distributedBatches)
    {
        ConcurrentBag<MailBoxDto> mailBoxDtos = new();
        var taskId2 = _taskManager.StartBatch(distributedBatches, async (emailAccount, cancellationToken) =>
        {
            if (emailAccount == null) throw new ArgumentNullException(nameof(emailAccount));
            ReturnTypeObject result = await _mailBoxRequests.DeleteAliases(emailAccount, IsdeleteExtra);
            return result.Message;
        }, batchSize: ReportingSettingsValuesDisplay.MailBoxThread);

        await CreateAnActiveTask(TaskCategory.Active, TaskCategory.Saved, TakInfoType.Batch, taskId2, "Delete Aliases",
            Statics.AliaseDeleteColor, Statics.AliaseDeleteSoftColor, distributedBatches.ToObservableCollection());
        await _taskManager.WaitForTaskCompletion(taskId2);


        #region reset packs to unactive after deleting aliases
        if (!IsDataLoaded)
        {
            var requestPayload = new
            {
                packNumber = SelectedTotalPackToSwitch,
                emailAddresses = distributedBatches
                    .Where(f => f?.EmailAddress != null)
                    .Select(f => f.EmailAddress)
                    .Distinct()
                    .ToList()
            };
            var apiCreateEmails = _taskManager.StartTask(async cancellationToken =>
            {
                await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.DeActivateMailboxesOnDelete, requestPayload);
            });
            await CreateAnActiveTask(TaskCategory.Invincible, TaskCategory.Invincible, TakInfoType.Single, apiCreateEmails,
                "Reset pack", Statics.UploadFileColor, Statics.UploadFileSoftColor, null);

        #endregion
        

            await _taskManager.WaitForTaskCompletion(apiCreateEmails);
        }
        #endregion
        return mailBoxDtos.ToList();
    }
    [RelayCommand]
    private async Task DeleteAliases()
    {
        try
        {
            SuccessMessage = "";
            var emailsGroupToWork = new List<EmailAccount>(FilterEmailsBySelectedGroups());
            var distributedBatches = proxyListManager.DistributeEmailsBySubnetSingleList(emailsGroupToWork);

            if (!distributedBatches.Any())
            {
                ErrorIndicator = new ErrorIndicatorViewModel();
                await ErrorIndicator.ShowErrorIndecator("Process Issue", "The group selected contains no emails.");
                return;
            }
            var aliases  = await GetAliasesAsyncWithoutApiUpdate(distributedBatches);
            SuccessMessage = "we Successfully Updated database ";
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Issue", e.Message);
        }
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

        await CreateAnActiveTask(TaskCategory.Invincible, TaskCategory.Invincible, TakInfoType.Single, apiCreateEmails2,
            "Add Mailboxes to api", Statics.ReportingColor, Statics.ReportingSoftColor, null);
        await _taskManager.WaitForTaskCompletion(apiCreateEmails2);
    }
    private async Task GetNickNameAsync(List<EmailAccount> distributedBatches)
    {
        ConcurrentBag<NetworkLogDto> networkLogEntries = new();
        var taskId = _taskManager.StartBatch(distributedBatches, async (emailAccount, cancellationToken) =>
        {
            if (emailAccount == null) throw new ArgumentNullException(nameof(emailAccount));
            ReturnTypeObject result = await _mailBoxRequests.PrepareMailBoxNickName(emailAccount);
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
    private async Task CreateAliasesAsync(List<EmailAccount> distributedBatches,List<NetworkLogDto> networkLogs , IEnumerable<EmailAccountWithPackAliases>? aliases = null)
    {
        ConcurrentBag<NetworkLogDto> networkLogEntries = new();
        var taskId = _taskManager.StartBatch(distributedBatches, async (emailAccount, cancellationToken) =>
        {
            if (emailAccount == null) throw new ArgumentNullException(nameof(emailAccount));
            var networkLog = networkLogs.FirstOrDefault(n => n.EmailId == emailAccount.Id);
            if (networkLog == null) throw new NullReferenceException("Nickname is null");
            List<string> aliasestopass = null;
            /*if (networkLog.mailboxCount >= 3) return "this mailbox already have 3 aliases";*/
            if (aliases != null)
            {
                aliasestopass = aliases
                    .Where(e => e.Account.EmailAddress == emailAccount.EmailAddress)
                    .SelectMany(x => x.PackAliasesToSwitch) // Flatten List<List<string>> to List<string>
                    .ToList();
            }
            ReturnTypeObject result = await _mailBoxRequests.CreateAliaseManagerInitializer(
                emailAccount, 
                Count, 
                networkLog.NickName, 
                CustomName, 
                aliasestopass
            );
            return $"Sucessfully Created Alias Manager \n {result.ReturnedValue}";
        }, batchSize: ReportingSettingsValuesDisplay.MailBoxThread);

        await CreateAnActiveTask(TaskCategory.Active,TaskCategory.Saved, TakInfoType.Batch, taskId, "Create MailBox",
            Statics.ReportingColor, Statics.ReportingSoftColor, distributedBatches.ToObservableCollection());
        await _taskManager.WaitForTaskCompletion(taskId);
    }

    #endregion
    private async Task RefreachAndCleanMailBoxAync()
    {
        try
        {
          
            var emailsGroupToWork = new List<EmailAccount>(FilterEmailsBySelectedGroups());
            var distributedBatches = proxyListManager.DistributeEmailsBySubnetSingleList(emailsGroupToWork);
            SuccessMessage = $"Clear and populate has been started  working on {distributedBatches.Count}";
            if (!distributedBatches.Any())
            {
                ErrorIndicator = new ErrorIndicatorViewModel();
                await ErrorIndicator.ShowErrorIndecator("Process Issue", "The group selected contains no emails.");
                return;
            }
            var requestPayload = new
            {
                packNumber = SelectedTotalPackToSwitch,
                emailAddresses = distributedBatches
                    .Where(f => f?.EmailAddress != null)
                    .Select(f => f.EmailAddress)
                    .Distinct()
                    .ToList()
            };
            var apiCreateEmails = _taskManager.StartTask(async cancellationToken =>
            {
                await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.DeleteAllMailboxes, requestPayload);
            });
            await CreateAnActiveTask(TaskCategory.Invincible, TaskCategory.Invincible, TakInfoType.Single, apiCreateEmails,
                "Clear pack", Statics.UploadFileColor, Statics.UploadFileSoftColor, null);
            await _taskManager.WaitForTaskCompletion(apiCreateEmails);
            await CollectMailboxesAsync(distributedBatches);
            await LoadEmailDetailsAsync();
            SuccessMessage = "we Successfully Updated database ";
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Issue", e.Message);
        }
    }
    private async Task InitializeMailBoxAync()
    {
        try
        {
            SuccessMessage = "";
            await LoadEmailsFromDb();

            var emailsGroupToWork = new List<EmailAccount>(FilterEmailsBySelectedGroups());
            var distributedBatches = proxyListManager.DistributeEmailsBySubnetSingleList(emailsGroupToWork);
            SuccessMessage = $"InitializeMailBox has been started  working on {distributedBatches.Count}";
            if (!distributedBatches.Any())
            {
                ErrorIndicator = new ErrorIndicatorViewModel();
                await ErrorIndicator.ShowErrorIndecator("Process Issue", "The group selected contains no emails.");
                return;
            }

            SuccessMessage = "UPDATE Mailboxes aliases STARTED";
            await CollectMailboxesAsync(distributedBatches);
            SuccessMessage = "UPDATE Mailboxes aliases Finished";
           
            SuccessMessage = "Collecting Mailboxes nickname STARTED";
            await GetNickNameAsync(distributedBatches);
            SuccessMessage = "Collecting Mailboxes nickname Finished";
            var networkLogs = await _apiConnector.GetDataAsync<IEnumerable<NetworkLogDto>>(ApiEndPoints.GetAllMailBoxes, ignoreCache:false);
            
           
            if (Count > 0)
            {
                try
                {
                    SuccessMessage = "";
                    var aliases  = await GetAliasesAsyncWithoutApiUpdate(distributedBatches);
                    SuccessMessage = "we Successfully deleted aliases";
                }
                catch (Exception e)
                {
                    ErrorIndicator = new ErrorIndicatorViewModel();
                    await ErrorIndicator.ShowErrorIndecator("Delete Aliase issues", e.Message);
                }
                SuccessMessage = "Creating  Mailboxes Aliases STARTED";
                await CreateAliasesAsync(distributedBatches,networkLogs.ToList());
                SuccessMessage = "Creating  Mailboxes Aliases Finished";
            }
          
            await CollectMailboxesAsync(distributedBatches);
            await LoadEmailDetailsAsync();
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
                            Title = e.Error?.Message.GetValueBetweenBrackets(),
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
    
    [RelayCommand]
    private void ExportToTxt()
    {
        try
        {
            // Get the filtered email list based on selected groups and store email addresses in a HashSet
            var filteredEmails = new HashSet<EmailAccount>(FilterEmailsBySelectedGroups());

            if (!filteredEmails.Any())
            {
                ErrorMessages = "No emails found for the selected groups.";
                return;
            }

            // Filter EmailsMailboxesDetailsTable using the email addresses from the selected groups
            var mailboxEmailAddresses = EmailsMailboxesDetailsTable
                .Select(details => details.EmailAddress)
                .ToHashSet();

            var emailsToExport = filteredEmails
                .Where(account => mailboxEmailAddresses.Contains(account.EmailAddress))
                .ToList();
            if (!emailsToExport.Any())
            {
                ErrorMessages = "No matching emails found in the details table.";
                return;
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "EmailsExport.txt");

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var item in emailsToExport)
                {
                    string line = $"{item.EmailAddress};{item.Password};;{item.Proxy?.ProxyIp};{item.Proxy?.Port};{item.Proxy?.Username};{item.Proxy?.Password}";
                    writer.WriteLine(line);
                }
            }

            SuccessMessage = $"Exported to {filePath}";
        }
        catch (Exception ex)
        {
            ErrorMessages = $"Error exporting file: {ex.Message}";
        }
    }
    [RelayCommand]
    private void ExportToTxtAliases()
    {
        try
        {
            // Get the filtered email list based on selected groups and store email addresses in a HashSet
            var filteredEmails = new HashSet<string>(FilterEmailsBySelectedGroups()?.Select(e => e.EmailAddress) ?? Enumerable.Empty<string>());

            if (!filteredEmails.Any())
            {
                ErrorMessages = "No emails found for the selected groups.";
                return;
            }

            // Filter EmailsMailboxesDetailsTable using the email addresses from the selected groups
            var emailsToExport = EmailsMailboxesDetailsTable
                .Where(details => filteredEmails.Contains(details.EmailAddress))
                .ToList();

            if (!emailsToExport.Any())
            {
                ErrorMessages = "No matching emails found in the details table.";
                return;
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "EmailsExport.txt");

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var item in emailsToExport)
                {
                    string line = $"{item.EmailAddress};{item.ActiveAliasesRaw}";
                    writer.WriteLine(line);
                }
            }

            SuccessMessage = $"Exported to {filePath}";
        }
        catch (Exception ex)
        {
            ErrorMessages = $"Error exporting file: {ex.Message}";
        }
    }

    #region Filter Table
        [ObservableProperty] private int _countFilter;
        [ObservableProperty] private EmailGroup? _selectedEmailGroup = null;
        [ObservableProperty] private bool _isGridPopupOpen = false;
        [ObservableProperty] private int _aliasCountFilterIndivitual;
        [ObservableProperty] private string _searchLabel = "";
        [ObservableProperty] private ObservableCollection<EmailGroup> _selectedEmailGroupForTask = new (); 
        
        
        [RelayCommand]
        private void ResetFilterTable()
        {
            SelectedEmailGroupForTask.Clear();
            EmailsMailboxesDetailsTable = new ObservableCollection<EmailMailboxDetails>(
                EmailsMailboxesDetails
                    .OrderByDescending(e => e.MailboxesCount ?? 0) 
            );
            CountFilter = EmailsMailboxesDetailsTable.Count;
        }
        
        [RelayCommand]
        private void ToggleGridFilterPopup()
        {
            IsGridPopupOpen = !IsGridPopupOpen;
        }
        [ObservableProperty]
        private int _maxTotalPacks;    
        [ObservableProperty]
        private bool _hasNoActiveAliases;  

        [ObservableProperty]
        private int selectedTotalPackToFilter;    
        [ObservableProperty]
        private int selectedTotalPackToSwitch;
        [ObservableProperty]
        private int _MailboxesCount;
        partial void OnEmailsMailboxesDetailsTableChanged(ObservableCollection<EmailMailboxDetails> newValue)
        {
            // Sum all mailboxes across details
            MailboxesCount = newValue.Count;
        }

        partial void OnSelectedTotalPackToFilterChanged(int value)
        {
            try
            {
                Dispatcher.UIThread.Post(() => { IsDataLoaded = true; });
                if (!SelectedEmailGroupForTask.Any())
                {
                    SelectedEmailGroupForTask = new ObservableCollection<EmailGroup>(Groups);
                }
                ResetTableValuesBaseOnGroupSelection();
                var query = EmailsMailboxesDetailsTable.Where(email => email.TotalPacks >= value);
                EmailsMailboxesDetailsTable = new ObservableCollection<EmailMailboxDetails>(
                    query.OrderByDescending(e => e.MailboxesCount ?? 0)
                );
            }
            finally
            {

                Dispatcher.UIThread.Post(() => { IsDataLoaded = false; });
            }

        } 
        partial void OnSearchLabelChanged(string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    EmailsMailboxesDetailsTable = new ObservableCollection<EmailMailboxDetails>(EmailsMailboxesDetails);
                }
                else
                {
                    var query = EmailsMailboxesDetailsTable.Where(email => email.EmailAddress.Contains(value) );
                    EmailsMailboxesDetailsTable = new ObservableCollection<EmailMailboxDetails>(
                        query.OrderByDescending(e => e.MailboxesCount ?? 0)
                    );
                 
                }
            
            }
            finally
            {
                
            }

        }  
        partial void OnHasNoActiveAliasesChanged(bool value)
        {
                Task.Run(() =>
                {
                    try
                    {
                        
                        Dispatcher.UIThread.Post(() =>
                        {
                            IsDataLoaded = true;
                        });
                        IEnumerable<EmailMailboxDetails> query = new[] { new EmailMailboxDetails() };
                        ResetTableValuesBaseOnGroupSelection();
                        query = value
                            ? EmailsMailboxesDetailsTable.Where(email => email.ActivePackNumber <= 0)
                            :  new ObservableCollection<EmailMailboxDetails>(EmailsMailboxesDetails);
                        EmailsMailboxesDetailsTable = new ObservableCollection<EmailMailboxDetails>(
                            query.OrderByDescending(e => e.MailboxesCount ?? 0)
                        );
                    }
                    finally 
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            IsDataLoaded = false;
                        });
                     
                    }
                  
                });
      
          
           
        }
        partial void OnAliasCountFilterIndivitualChanged(int value)
        {
                Task.Run(() =>
                {
                    try
                    {
                        
                        Dispatcher.UIThread.Post(() =>
                        {
                            IsDataLoaded = true;
                        });
                        IEnumerable<EmailMailboxDetails> query = new[] { new EmailMailboxDetails() };
                        ResetTableValuesBaseOnGroupSelection();
                        
                        if (value == 0)
                        {
                            query = EmailsMailboxesDetailsTable.Where(email => email.ActiveAliases.Count <= 0);
                        }
                        else
                        {
                            query = EmailsMailboxesDetailsTable.Where(email => email.ActiveAliases.Count == value);
                        }
                        EmailsMailboxesDetailsTable = new ObservableCollection<EmailMailboxDetails>(
                            query.OrderByDescending(e => e.MailboxesCount ?? 0)
                        );
                    }
                    finally 
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            IsDataLoaded = false;
                        });
                     
                    }
                  
                });
      
          
           
        }
        private void OnSelectedGroupsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsDataLoaded = true;
                });
                Task.Run(() =>
                {
                    EmailDiaplysTable = FilterEmailsBySelectedGroups(false);
                    var result = EmailsMailboxesDetails
                        .Where(d => EmailDiaplysTable.Any(b => b.EmailAddress == d.EmailAddress)).ToObservableCollection();
                    EmailsMailboxesDetailsTable = result;
                });
            }
            finally
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsDataLoaded = false;
                });
            }
            // Refresh the filtered list whenever the selection changes
       
        }

        private void ResetTableValuesBaseOnGroupSelection()
        {
            EmailDiaplysTable = FilterEmailsBySelectedGroups(false);
            var result = EmailsMailboxesDetails
                .Where(d => EmailDiaplysTable.Any(b => b.EmailAddress == d.EmailAddress)).ToObservableCollection();
            EmailsMailboxesDetailsTable = result;
          
        }
        public ObservableCollection<EmailAccount> FilterEmailsBySelectedGroups(bool working = true)
        {
            try
            {
                /*
                if (!SelectedEmailGroupForTask.Any())
                {
                    // If no groups are selected, return an empty collection or the full list, depending on your requirements
                    return new ObservableCollection<EmailAccount>(EmailDiaplysTable);
                }
                */
                
                if (working)
                {   
                    var result = EmailAccounts?
                    .Where(d => EmailsMailboxesDetailsTable.Any(b => b.EmailAddress == d.EmailAddress)).ToList();
                 
                    return new ObservableCollection<EmailAccount>(result!.ToObservableCollection());
                }
                else
                {
                    var filteredEmails = EmailAccounts
                        ?.Where(email => SelectedEmailGroupForTask.Any(group => group.GroupId == email.Group.GroupId))
                        .ToList();
                    return new ObservableCollection<EmailAccount>(filteredEmails!.ToObservableCollection());
                }
             
            }
            catch (Exception e)
            {
                throw new Exception($"FilterEmailsBySelectedGroups failed: {e.Message}");
            }
        
        }
    #endregion


    #region Switch

    [RelayCommand]
    private async Task SwitchPacks()
    {
        SuccessMessage = "";
        if (!EmailsMailboxesDetailsTable.Any())
        {
            SuccessMessage = "no email selected";
            return;
        }
        List<EmailAccountWithPackAliases?> filtered = EmailsMailboxesDetailsTable
            .Where(details =>
                details.PackAliases.TryGetValue(SelectedTotalPackToSwitch, out var packAliases) &&
                packAliases.Any(alias => !(details.ActiveAliasesRaw ?? "").Contains(alias)))
            .Select(details =>
            {
                var matchingAccount = NetworkItems.FirstOrDefault(acc =>
                    acc.EmailAddress.Equals(details.EmailAddress, StringComparison.CurrentCultureIgnoreCase));

                if (matchingAccount != null)
                {
                    var packAliasesToSwitch = details.PackAliases[SelectedTotalPackToSwitch]
                        .Where(alias => !(details.ActiveAliases ?? new List<string>()).Contains(alias))
                        .ToList();

                    return new EmailAccountWithPackAliases()
                    {
                        Account = matchingAccount,
                        PackAliasesToSwitch = packAliasesToSwitch
                    };
                }

                return null;
            })
            .Where(x => x != null)
            .ToList();


        if (!filtered.Any())
        {
            SuccessMessage = "no email accounts found.";
            return;
        }

        // todo:  delete all existing aliases in an account 
        var query =  proxyListManager.DistributeEmailsBySubnetSingleList(
            filtered
                .Where(email => email.Account != null)
                .Select(email => email.Account!)
                .ToList()
        );
        try
        {
            SuccessMessage = "";
            var distributedBatches = query;
            
            var aliases  = await GetAliasesAsyncWithoutApiUpdate(distributedBatches);
            SuccessMessage = "we Successfully deleted aliases";
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Delete Aliase issues", e.Message);
        }
        // todo: add the new aliases in the account 
        SuccessMessage = "Creating  new  Mailboxes Aliases STARTED";
        var networkLogs = await _apiConnector.GetDataAsync<IEnumerable<NetworkLogDto>>(ApiEndPoints.GetAllMailBoxes, ignoreCache:false);
        await CreateAliasesAsync(query,networkLogs.ToList(),filtered);
        SuccessMessage = "Creating  new Mailboxes Aliases Finished";
        // todo: switch active aliases in database
        var requestPayload = new
        {
            packNumber = SelectedTotalPackToSwitch,
            emailAddresses = filtered
                .Where(f => f?.Account?.EmailAddress != null)
                .Select(f => f.Account.EmailAddress)
                .Distinct()
                .ToList()
        };

        var apiCreateEmails = _taskManager.StartTask(async cancellationToken =>
        {
            await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.ActivatePack, requestPayload);
        });
        await CreateAnActiveTask(TaskCategory.Invincible, TaskCategory.Invincible, TakInfoType.Single, apiCreateEmails,
            "Activate pack", Statics.UploadFileColor, Statics.UploadFileSoftColor, null);

        await _taskManager.WaitForTaskCompletion(apiCreateEmails);
        
      
        await CollectMailboxesAsync(query);
        await LoadEmailDetailsAsync();
    }

    #endregion
}