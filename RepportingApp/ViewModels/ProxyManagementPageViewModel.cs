


using System.Diagnostics;
using Reporting.lib.Models.DTO;
using RepportingApp.CoreSystem.ProxyService;

namespace RepportingApp.ViewModels;

public partial class ProxyManagementPageViewModel : ViewModelBase,ILoadableViewModel
{


    #region Timer

    private readonly DispatcherTimer _timer;
    private TimeSpan _interval = TimeSpan.FromMinutes(30);

    [ObservableProperty]
    private int _intervalMinutes = 30; 


    #endregion
    
    
    [ObservableProperty] private bool _isDataLoaded = false;
    [ObservableProperty] private bool _isProcessWorking = false;
    private readonly TaskInfoManager _taskInfoManager;
    [ObservableProperty] private ReportingSettingValues _reportingSettingsValuesDisplay;
    [ObservableProperty] public ErrorIndicatorViewModel _errorIndicator= new ErrorIndicatorViewModel();
    [ObservableProperty] private int _countFilter;
    [ObservableProperty] 
    public ObservableCollection<Proxy> centralProxyList = new ObservableCollection<Proxy>();
    [ObservableProperty] 
    public ObservableCollection<Proxy> copyCentralProxyList = new ObservableCollection<Proxy>();
    [ObservableProperty] public ObservableCollection<Proxy> _selectedProxies;
    [ObservableProperty] private ObservableCollection<EmailAccount> _emailAccounts = new ObservableCollection<EmailAccount>();
    private readonly IApiConnector _apiConnector;
    private readonly IProxyApiService _proxyApiService;
    [ObservableProperty]
    private ObservableCollection<string> regions;

    [ObservableProperty]
    private ObservableCollection<string> subnets;

    [ObservableProperty]
    private ObservableCollection<string> availabilities;

    [ObservableProperty]
    private ObservableCollection<string> connectivities;
    [ObservableProperty]
    private string selectedRegion;

    [ObservableProperty]
    private string selectedSubnet; 

    [ObservableProperty]
    private string selectedAvailability; 

    [ObservableProperty]
    private string selectedConnectivity; 
    
    
    [ObservableProperty]
    private bool isAllSelected;
    private UnifiedTaskManager _taskManager;
    public ProxyManagementPageViewModel(IMessenger messenger  , IApiConnector apiConnector, TaskInfoManager taskInfoManager,IProxyApiService proxyApiService) : base(messenger)
    {
        _apiConnector = apiConnector;
        _taskInfoManager = taskInfoManager;
        _proxyApiService = proxyApiService;
        if (App.Configuration != null) ReportingSettingsValuesDisplay = new ReportingSettingValues(App.Configuration);
        
        Regions = new ObservableCollection<string> { "Select Region" }; // Placeholder item
        Subnets = new ObservableCollection<string> { "Select Subnet" }; // Placeholder item
        Availabilities = new ObservableCollection<string> { "availability", "used", "available" };
        Connectivities = new ObservableCollection<string> { "Connectivity", "Google", "Yahoo" };
        SelectedRegion = "Select Region";
        SelectedSubnet = "Select Subnet";
        SelectedAvailability = "availability";
        SelectedConnectivity = "Connectivity";
       
        InitializeTaskManager();
        _timer = new DispatcherTimer();
        _timer.Tick += async (s, e) => await GetAllProxiesFromApi();
        
        StartTimer();
        
        
    }

    #region Timer

    private void StartTimer()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }

        _interval = TimeSpan.FromMinutes(IntervalMinutes);
        _timer.Interval = _interval;
        _timer.Start();
    }
    [RelayCommand]
    private void UpdateCycleInterval()
    {
        StartTimer();
    }

    #endregion
   
    private void InitializeTaskManager()
    {
        var systemEstimator = new SystemConfigurationEstimator();
        _taskManager = new UnifiedTaskManager(systemEstimator.RecommendedMaxDegreeOfParallelism, _taskInfoManager);
    }
    public async Task LoadDataIfFirstVisitAsync(bool ignorecache = false)
    {
        try
        {
            if (!IsDataLoaded)
            {
                IsDataLoaded = true;
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
            IsDataLoaded = false;
        }
    }

    private async Task LoadDataAsync()
    {
        var emails = await _apiConnector.GetDataAsync<IEnumerable<EmailAccount>>(ApiEndPoints.GetEmails,ignoreCache:false);
        var proxies = await _apiConnector.GetDataAsync<IEnumerable<Proxy>>(ApiEndPoints.GetAllProxies,ignoreCache:false);
        EmailAccounts = emails.ToObservableCollection();
        CentralProxyList = proxies.ToObservableCollection();
        CopyCentralProxyList = new ObservableCollection<Proxy>(CentralProxyList);
        CountFilter = CopyCentralProxyList.Count;
    }
    
    [RelayCommand]
    private async Task ProcessAStart()
    {
        _messenger.Send(new ProcessStartMessage("Success","Process B ",new ProcessModel()));
    }
    
    public async Task ShowIndicator(string title,string message)
    {
        try
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator(title, message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    
  
    }

    [RelayCommand]
    private async Task StartTestProxies()
    {
        try
        {
            if (SelectedProxies == null) return;
            if (!SelectedProxies.Any())
            {
                return;
            }
            var taskId = _taskManager.StartBatch(SelectedProxies, async (selectedProxy, cancellationToken) =>
            {
            
                await ProxyListManager.TestProxiesAsync(selectedProxy);
                return null;
            }, batchSize:200,taskCategory: TaskCategory.Invincible);

            await _taskManager.WaitForTaskCompletion(taskId);
            await PopulateFilters(CopyCentralProxyList);
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Fail", e.Message);
        }
      
    }
    
    
    public async Task PopulateFilters(IEnumerable<Proxy> proxies)
    {
        try
        {
            var centralProxies = proxies as Proxy[] ?? proxies.ToArray();

            // Populate regions
            ClearExceptFirst(Regions);
            foreach (var region in centralProxies.Select(p => p.Region).Distinct())
            {
                Regions.Add(region);
            }

            // Populate subnets
            ClearExceptFirst(Subnets);
            foreach (var subnet in GetDistinctSubnets(centralProxies))
            {
                Subnets.Add(subnet);
            }
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Fail", e.Message);
        }
      
    }

    private void ClearExceptFirst(ObservableCollection<string> collection)
    {
        if (collection.Count > 1)
        {
            var firstItem = collection[0];
            collection.Clear();
            collection.Add(firstItem);
        }
    }

    private IEnumerable<string> GetDistinctSubnets(IEnumerable<Proxy> proxies)
    {
        // Example logic to calculate subnets
        return proxies.Select(p => p.ProxyIp.Split('.').Take(3).Aggregate((a, b) => $"{a}.{b}")).Distinct();
    }
    
    [RelayCommand]
    public async Task ApplyFilter()
    {
        try
        {
            // Start with the original unfiltered list
            var filtered = CentralProxyList.AsEnumerable();

            // Filter based on selected values
            if (!string.IsNullOrEmpty(SelectedRegion) && SelectedRegion != "Select Region")
                filtered = filtered.Where(p => p.Region == SelectedRegion);

            if (!string.IsNullOrEmpty(SelectedSubnet) && SelectedSubnet != "Select Subnet")
                filtered = filtered.Where(p => GetSubnet(p.ProxyIp) == SelectedSubnet);

            if (!string.IsNullOrEmpty(SelectedAvailability) && SelectedAvailability != "availability")
                filtered = filtered.Where(p => SelectedAvailability == "used" ? !p.Availability : p.Availability);

            if (!string.IsNullOrEmpty(SelectedConnectivity) && SelectedConnectivity != "Connectivity")
                filtered = filtered.Where(p => SelectedConnectivity == "Google" ? p.GoogleConnectivity == "Connected" : p.YahooConnectivity == "Connected");

            // Update filtered list
            var centralProxies = filtered as Proxy[] ?? filtered.ToArray();
            CopyCentralProxyList.Clear();
            foreach (var proxy in centralProxies)
                CopyCentralProxyList.Add(proxy);

            // Update count of filtered results
            CountFilter = CopyCentralProxyList.Count;
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Fail", e.Message);
        }
      
    }

    private string GetSubnet(string ip)
    {
        // Example subnet calculation logic
        return string.Join('.', ip.Split('.').Take(3));
    }
    [RelayCommand]
    public void Reset()
    {
        CopyCentralProxyList= new ObservableCollection<Proxy>(CentralProxyList);
        CountFilter = CopyCentralProxyList.Count;
    }

    [RelayCommand]
    private async Task ImportProxyFromFile()
    {
        try
        {
            
            ProxyListManager.UploadReservedProxyFile();

          
            var existingProxies = new HashSet<string>(CopyCentralProxyList.Select(p => $"{p.ProxyIp}:{p.Port}"));

            
            var newProxies = ProxyListManager.ReservedProxies
                .Where(proxy =>
                {
                    var proxyKey = $"{proxy.ProxyIp}:{proxy.Port}";
                    return !existingProxies.Contains(proxyKey);
                })
                .ToList();
            
          
            if (newProxies.Any())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    foreach (var proxy in newProxies)
                    {
                        CopyCentralProxyList.Add(proxy);
                    }

                
                    CountFilter = CopyCentralProxyList.Count;
                });
            }
            var proxyDtos = newProxies.Select(p => new ProxyDto
            {
                ProxyIp = p.ProxyIp,
                Port = p.Port,
                Username = p.Username,
                Password = p.Password,
                Availability = "Available"
            });
            await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.PostProxy,proxyDtos);
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Fail", e.Message);
        }
      
    }

    [RelayCommand]
    public async Task AssignProxies()
    {
        try
        {
            IsProcessWorking = true;
            var proxies = await _apiConnector.GetDataAsync<IEnumerable<Proxy>>(ApiEndPoints.GetAllProxies, ignoreCache: false);
            var newProxies = proxies.Where(e => e.Availability);
            if (!newProxies.Any())throw new Exception("available proxies File is empty please import ");
            if (!EmailAccounts.Any())throw new Exception("there is no emails account");
            var newProxySet = new HashSet<string>(newProxies.Select(p => p.ProxyIp));
            var availableProxies = new Queue<Proxy>(newProxies);

            var removedProxies = new List<Proxy>();
            var removedProxySet = new HashSet<string>();
            var updatedMappings = new List<EmailProxyMappingDto>(); // DTO list for updates

            foreach (var email in EmailAccounts)
            {
                if (email.Proxy != null && !newProxySet.Contains(email.Proxy.ProxyIp))
                {
                    if (removedProxySet.Add(email.Proxy.ProxyIp))
                    {
                        removedProxies.Add(email.Proxy);
                    }

                    if (availableProxies.TryDequeue(out var newProxy))
                    {
                        
                        updatedMappings.Add(new EmailProxyMappingDto
                        {
                            EmailAddress = email.EmailAddress,
                            ProxyIp = newProxy.ProxyIp,
                            Port = newProxy.Port
                        });

                        // Replace the proxy in the email object
                        email.Proxy = newProxy;
                    }
                    else
                    {

                    }
                }
            }

            foreach (var proxy in removedProxies)
            {
                CentralProxyList.Remove(proxy);
            }

            foreach (var proxy in newProxies)
            {
                if (!CentralProxyList.Any(p => p.ProxyIp == proxy.ProxyIp))
                {
                    CentralProxyList.Add(proxy);
                }
            }

            await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.PostEmailsProxiesUpdate, updatedMappings);
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Fail", e.Message);
        }
        finally
        {
            IsProcessWorking = false;
        }
        

    }

    [RelayCommand]
    private async Task ReplaceProxies()
    {
        try
        {
            IsProcessWorking = true;
            var result = await _proxyApiService.GetAllReplacedProxiesAsync();
            var proxyDtos = result.Select(proxy => new ProxyUpdateDto
            {
                OldProxyIp = proxy.proxy,
                OldProxyPort = proxy.proxy_port,
                NewProxyIp = proxy.replaced_with,
                NewProxyPort = proxy.replaced_with_port
                
            }).ToList();
            await _apiConnector.PostDataObjectAsync<string>(ApiEndPoints.ReplaceProxyProxy, proxyDtos);
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("ReplaceProxies Fail", e.Message);
        }
        finally
        {
            IsProcessWorking = false;
        }
      
    }

    [RelayCommand]
    private async Task GetAllProxiesFromApi()
    {
        try
        {
            await ReplaceProxies();
            IsProcessWorking = true;
            await _proxyApiService.DownloadProxyListAsync();
            var uploadedFiles = await ProxyListManager.UploadNewProxyFileAsync();
            var proxyDtos = uploadedFiles.Select(p => new ProxyDto
            {
                ProxyIp = p.ProxyIp,
                Port = p.Port,
                Username = p.Username,
                Password = p.Password,
                Availability = "Available"
            });
            await _apiConnector.PostDataObjectAsync<string>(ApiEndPoints.PostProxy, proxyDtos);
            await LoadDataAsync();
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("GetAllProxiesFromApi Fail", e.Message);
        }
        finally
        {
            IsProcessWorking = false;
        }
      
         
    }


}

