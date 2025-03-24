


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
    [ObservableProperty] private bool _isTestingQuality = false;
    [ObservableProperty] private bool _isCleanAllBeforeAssign = false;
    [ObservableProperty] private bool _isAssignWorking = false;
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
    private readonly ProxyListManager proxyListManager;
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

    [ObservableProperty] private string _selectedConnectivity = "Connectivity"; // Default
    
    
    [ObservableProperty]
    private bool isAllSelected;
    private UnifiedTaskManager _taskManager;
    public ProxyManagementPageViewModel(IMessenger messenger  , IApiConnector apiConnector, TaskInfoManager taskInfoManager,IProxyApiService proxyApiService) : base(messenger)
    {
        _apiConnector = apiConnector;
        _taskInfoManager = taskInfoManager;
        _proxyApiService = proxyApiService;
        proxyListManager = new ProxyListManager();
        if (App.Configuration != null) ReportingSettingsValuesDisplay = new ReportingSettingValues(App.Configuration);
        
        Regions = new ObservableCollection<string> { "Select Region" }; // Placeholder item
        Subnets = new ObservableCollection<string> { "Select Subnet" }; // Placeholder item
        Availabilities = new ObservableCollection<string> { "availability", "used", "available" };
        Connectivities = new ObservableCollection<string> { "Connectivity", "Working", "Failed" };
        SelectedRegion = "Select Region";
        SelectedSubnet = "Select Subnet";
        SelectedAvailability = "availability";
        SelectedConnectivity = "Connectivity";
       
        InitializeTaskManager();
        _timer = new DispatcherTimer();
        _timer.Tick += async (s, e) => await GetAllProxiesFromApi();
        
        StartTimer();
        proxyListManager.OnProxiesUpdated += UploadToApiProxiesRegion;

    }

    #region Timer

    #region events

    private  async void UploadToApiProxiesRegion(object? sender, List<Proxy> proxies)
    {
        await UploadProxiesToApiAsync(proxies);
    }

    public async Task UploadProxiesToApiAsync(List<Proxy> proxy)
    {
        try
        {
            var proxyDtos = proxy.Select(p => new ProxyUpdateRegion
            {
                ProxyId = p.ProxyId,
                Region = p.Region,
                YahooConnectivity = p.YahooConnectivity,
                Availability = p.Availability
            });
            var result = await _apiConnector.PostDataObjectAsync<string>(ApiEndPoints.UpdateProxy,proxyDtos);
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("UploadProxiesToApiAsync Fail", e.Message);
        }
       
    }

    #endregion
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
        var proxies = await _apiConnector.GetDataAsync<IEnumerable<Proxy>>(ApiEndPoints.GetAllProxies,ignoreCache:false);
        CentralProxyList = proxies.ToObservableCollection();
        CopyCentralProxyList = new ObservableCollection<Proxy>(CentralProxyList);
        CountFilter = CopyCentralProxyList.Count;
        await PopulateFilters(CopyCentralProxyList);
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

    /*[RelayCommand]
    private async Task StartTestProxies()
    {
        try
        {
            IsProcessWorking = true;
            IsTestingQuality = true;
            if (SelectedProxies == null || !SelectedProxies.Any()) return;

            // Convert ObservableCollection to List
            List<Proxy> proxyList = SelectedProxies.ToList();

            // Test proxies and fetch region data in batches
            await proxyListManager.TestProxiesAsync(proxyList);

            // Populate filters or update the UI
            await PopulateFilters(CopyCentralProxyList);
        }
        catch (Exception e)
        {
            // Handle errors
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("StartTestProxies", e.Message);
        }
        finally
        {
            IsTestingQuality = false;
            IsProcessWorking = false;
        }
    }*/

    
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

    #region Filter

    [ObservableProperty] private string _filterIpAddress = string.Empty;
    [RelayCommand]
    public async Task ExportFilteredData()
    {
        try
        {
            if (CopyCentralProxyList == null || !CopyCentralProxyList.Any())
            {
                await ErrorIndicator.ShowErrorIndecator("Export Failed", "No data to export.");
                return;
            }

            // Get the Desktop path
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "FilteredProxies.txt");

            // Format the data
            var lines = CopyCentralProxyList.Select(proxy => 
                $"{proxy.ProxyIp}:{proxy.Port}:{proxy.Username}:{proxy.Password}");

            // Write to the file
            await File.WriteAllLinesAsync(filePath, lines);
        }
        catch (Exception e)
        {
            await ErrorIndicator.ShowErrorIndecator("Export Failed", e.Message);
        }
    }
    [RelayCommand]
    public async Task ApplyFilter()
    {
        try
        {
            var filtered = CentralProxyList.AsEnumerable();

            // Filter by IP Address if it's not empty
            if (!string.IsNullOrEmpty(FilterIpAddress))
                filtered = filtered.Where(p => p.ProxyIp.Contains(FilterIpAddress));

            if (!string.IsNullOrEmpty(SelectedRegion) && SelectedRegion != "Select Region")
                filtered = filtered.Where(p => p.Region == SelectedRegion);

            if (!string.IsNullOrEmpty(SelectedSubnet) && SelectedSubnet != "Select Subnet")
                filtered = filtered.Where(p => GetSubnet(p.ProxyIp) == SelectedSubnet);

            if (!string.IsNullOrEmpty(SelectedAvailability) && SelectedAvailability != "availability")
                filtered = filtered.Where(p => SelectedAvailability == "used" ? !p.Availability : p.Availability);
            if (!string.IsNullOrEmpty(SelectedConnectivity) && SelectedConnectivity != "Connectivity")
                filtered = filtered.Where(p => p.YahooConnectivity == SelectedConnectivity);


            var centralProxies = filtered as Proxy[] ?? filtered.ToArray();
            CopyCentralProxyList.Clear();
            foreach (var proxy in centralProxies)
                CopyCentralProxyList.Add(proxy);

            CountFilter = CopyCentralProxyList.Count;
        }
        catch (Exception e)
        {
            await ErrorIndicator.ShowErrorIndecator("Process Fail", e.Message);
        }
    }
    #endregion
  

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
            
            proxyListManager.UploadReservedProxyFile();

          
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
                Availability = true
            });
            await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.PostProxy,proxyDtos);
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Process Fail", e.Message);
        }
      
    }
// Helper method to save failed proxies to a file on Desktop

    [RelayCommand]
    public async Task AssignProxies()
    {

        try
        {
            IsAssignWorking = true;
            var proxies = await _apiConnector.GetDataAsync<IEnumerable<Proxy>>(ApiEndPoints.GetAllProxies, ignoreCache: false);
            var emails = await _apiConnector.GetDataAsync<IEnumerable<EmailAccount>>(ApiEndPoints.GetEmails, ignoreCache: false);
            var enumerable = proxies.ToList();

            try
            {
                if (IsCleanAllBeforeAssign)
                {
                    int batchSize = 1000;
                    var proxyBatches = enumerable.Chunk(batchSize);

                    foreach (var batch in proxyBatches)
                    {
                        await proxyListManager.TestProxiesAsync(batch.ToList());
                    }


                }
            }
            catch (Exception e)
            {
              throw new Exception($"Clean Before Assign {e.Message}");
            }
            try
            { 
                IsProcessWorking = true;
                
                var unassignedEmails = emails.Where(e => e.Proxy == null).ToList();
                if (!unassignedEmails.Any())
                {
                    Console.WriteLine("No emails need proxy assignment.");
                    return;
                }
                // Create a modifiable list of proxies, sorted by usage count (Least used first)
                List<Proxy> proxyPool = enumerable.OrderBy(p => p.ProxyUsageCount).ToList();

                List<EmailProxyMappingDto> updatedMappings = new();

                foreach (var email in unassignedEmails)
                {
                    if (proxyPool.Count == 0)
                    {
                        Console.WriteLine($"No proxies left for email: {email.EmailAddress}");
                        break;
                    }

                    // Always pick the least-used proxy (first one in sorted list)
                    Proxy assignedProxy = proxyPool.First();
                    assignedProxy.ProxyUsageCount++; // Simulate incrementing usage count (must update DB later)

                    updatedMappings.Add(new EmailProxyMappingDto
                    {
                        EmailAddress = email.EmailAddress,
                        ProxyIp = assignedProxy.ProxyIp,
                        Port = assignedProxy.Port,
                    });

                    // Re-sort the proxy pool after updating usage count
                    proxyPool = proxyPool.OrderBy(p => p.ProxyUsageCount).ToList();
                }

                if (updatedMappings.Any())
                {
                    await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.PostEmailsProxiesUpdate, updatedMappings);
                }
                
            }
            catch (Exception e)
            {
                throw new Exception($"Assign Fail: {e.Message}");
            }
            finally
            {
                IsProcessWorking = false;
            }
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("Assign", e.Message);
        }
        finally
        {
            IsAssignWorking = false;
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
    private async Task CleanEmailsProxy()
    {
        try
        {
            IsProcessWorking = true;
            await _apiConnector.PostDataObjectAsync<string>(ApiEndPoints.CleanEmailsProxies, string.Empty);
            await AssignProxies();
        }
        catch (Exception e)
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator("CleanEmailsProxy Fail", e.Message);
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
            if (IsAssignWorking || IsProcessWorking) return;
            await ReplaceProxies();
            IsProcessWorking = true;
            await _proxyApiService.DownloadProxyListAsync();
            var uploadedFiles = await proxyListManager.UploadNewProxyFileAsync();

            int batchSize = 1000; // Adjust based on your needs
            var proxyBatches = await DistributeProxiesBySubnet(uploadedFiles, batchSize);

            foreach (var batch in proxyBatches)
            {
                // Test proxies in parallel with subnet-aware rate limiting
                var testedBatch = await proxyListManager.TestProxiesAsync(batch, 3, false);

                // Filter working proxies
                var validProxies = testedBatch
                    .Where(p => p.YahooConnectivity == "Working")
                    .ToList();

                if (validProxies.Any())
                {
                    var proxyDtos = validProxies.Select(p => new ProxyDto
                    {
                        ProxyId = p.ProxyId > 0 ? p.ProxyId : null,
                        ProxyIp = p.ProxyIp,
                        Port = p.Port,
                        Username = p.Username,
                        Password = p.Password,
                        Availability = true,
                        Region = p.Region,
                        YahooConnectivity = p.YahooConnectivity
                    }).ToList();

                    // Upload working proxies immediately in batch
                    await _apiConnector.PostDataObjectAsync<string>(ApiEndPoints.PostProxy, proxyDtos);
                }
            }
            
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
    private async Task<List<List<Proxy>>> DistributeProxiesBySubnet(List<Proxy> proxies, int batchSize)
    {
        // Group proxies by subnet
        var groupedBySubnet = proxies.GroupBy(p => ProxyListManager.GetSubnet2(p.ProxyIp)).ToList();

        // Create an interleaved list of proxies to reduce same-subnet grouping
        var interleavedList = new List<Proxy>();
        int maxGroupSize = groupedBySubnet.Max(g => g.Count());

        for (int i = 0; i < maxGroupSize; i++)
        {
            foreach (var group in groupedBySubnet)
            {
                if (i < group.Count())
                    interleavedList.Add(group.ElementAt(i));
            }
        }

        // Create batches from the interleaved list
        return interleavedList.Chunk(batchSize).Select(chunk => chunk.ToList()).ToList();
    }



  

}

