


using RepportingApp.CoreSystem.ProxyService;

namespace RepportingApp.ViewModels;

public partial class ProxyManagementPageViewModel : ViewModelBase,ILoadableViewModel
{
    private bool _isDataLoaded = false;
    private readonly TaskInfoManager _taskInfoManager;
    [ObservableProperty] private ReportingSettingValues _reportingSettingsValuesDisplay;
    [ObservableProperty] public ErrorIndicatorViewModel _errorIndicator= new ErrorIndicatorViewModel();
    [ObservableProperty] private int _countFilter;
    [ObservableProperty] public ObservableCollection<CentralProxy> centralProxyList;
    [ObservableProperty] public ObservableCollection<CentralProxy> copyCentralProxyList;
    [ObservableProperty] public ObservableCollection<CentralProxy> _selectedProxies;
    
    
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
    public ProxyManagementPageViewModel(IMessenger messenger  , TaskInfoManager taskInfoManager) : base(messenger)
    {
        _taskInfoManager = taskInfoManager;
        if (App.Configuration != null) ReportingSettingsValuesDisplay = new ReportingSettingValues(App.Configuration);
        CentralProxyList = new ObservableCollection<CentralProxy>
        {
            new CentralProxy(true, "206.41.179.47", "5723", "reda2816", "reda2816", null, null, null, null, true),
            new CentralProxy(false, "206.41.179.105", "5781", "reda2816", "reda2816", null, null, null, null, false),
            new CentralProxy(true, "192.168.1.3", "8082", "user3", "pass3", "25ms", "Connected", "Connected", "CA", true),
            new CentralProxy(false, "192.168.1.4", "8083", "user4", "pass4", "30ms", "Not Connected", "Not Connected", "AU", false),
            new CentralProxy(true, "192.168.1.5", "8084", "user5", "pass5", "35ms", "Connected", "Connected", "IN", true)
        };
        CopyCentralProxyList = new ObservableCollection<CentralProxy>(CentralProxyList);
        Regions = new ObservableCollection<string> { "Select Region" }; // Placeholder item
        Subnets = new ObservableCollection<string> { "Select Subnet" }; // Placeholder item
        Availabilities = new ObservableCollection<string> { "availability", "used", "available" };
        Connectivities = new ObservableCollection<string> { "Connectivity", "Google", "Yahoo" };
        SelectedRegion = "Select Region";
        SelectedSubnet = "Select Subnet";
        SelectedAvailability = "availability";
        SelectedConnectivity = "Connectivity";
        CountFilter = CopyCentralProxyList.Count;
        InitializeTaskManager();
    }
    private void InitializeTaskManager()
    {
        var systemEstimator = new SystemConfigurationEstimator();
        _taskManager = new UnifiedTaskManager(systemEstimator.RecommendedMaxDegreeOfParallelism, _taskInfoManager);
    }
    public async Task LoadDataIfFirstVisitAsync(bool ignorecache = false)
    {
        if (!_isDataLoaded)
        {
            _isDataLoaded = true;
            // Load data here
            await LoadDataAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        // Your data loading logic here
    }
    
    [RelayCommand]
    private async Task ProcessAStart()
    {
        _messenger.Send(new ProcessStartMessage("Success","Process B ",new ProcessModel()));
        await ShowIndicator("name", "something to show");
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
        var taskId = _taskManager.StartBatch(SelectedProxies, async (selectedProxy, cancellationToken) =>
        {
            
            await ProxyListManager.TestProxiesAsync(selectedProxy);
            return null;
        }, batchSize:200,taskCategory: TaskCategory.Invincible);

        await _taskManager.WaitForTaskCompletion(taskId);
        if(SelectedProxies.Count == CentralProxyList.Count)PopulateFilters(CopyCentralProxyList);
    }
    
    
    public void PopulateFilters(IEnumerable<CentralProxy> proxies)
    {
        var centralProxies = proxies as CentralProxy[] ?? proxies.ToArray();

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

    private void ClearExceptFirst(ObservableCollection<string> collection)
    {
        if (collection.Count > 1)
        {
            var firstItem = collection[0];
            collection.Clear();
            collection.Add(firstItem);
        }
    }

    private IEnumerable<string> GetDistinctSubnets(IEnumerable<CentralProxy> proxies)
    {
        // Example logic to calculate subnets
        return proxies.Select(p => p.Ip.Split('.').Take(3).Aggregate((a, b) => $"{a}.{b}")).Distinct();
    }
    
    [RelayCommand]
    public void ApplyFilter()
    {
        // Start with the original unfiltered list
        var filtered = CentralProxyList.AsEnumerable();

        // Filter based on selected values
        if (!string.IsNullOrEmpty(SelectedRegion) && SelectedRegion != "Select Region")
            filtered = filtered.Where(p => p.Region == SelectedRegion);

        if (!string.IsNullOrEmpty(SelectedSubnet) && SelectedSubnet != "Select Subnet")
            filtered = filtered.Where(p => GetSubnet(p.Ip) == SelectedSubnet);

        if (!string.IsNullOrEmpty(SelectedAvailability) && SelectedAvailability != "availability")
            filtered = filtered.Where(p => SelectedAvailability == "used" ? p.Availability : !p.Availability);

        if (!string.IsNullOrEmpty(SelectedConnectivity) && SelectedConnectivity != "Connectivity")
            filtered = filtered.Where(p => SelectedConnectivity == "Google" ? p.GoogleConnectivity == "Connected" : p.YahooConnectivity == "Connected");

        // Update filtered list
        var centralProxies = filtered as CentralProxy[] ?? filtered.ToArray();
        CopyCentralProxyList.Clear();
        foreach (var proxy in centralProxies)
            CopyCentralProxyList.Add(proxy);

        // Update count of filtered results
        CountFilter = CopyCentralProxyList.Count;
    }

    private string GetSubnet(string ip)
    {
        // Example subnet calculation logic
        return string.Join('.', ip.Split('.').Take(3));
    }
    [RelayCommand]
    public void Reset()
    {
        CopyCentralProxyList= new ObservableCollection<CentralProxy>(CentralProxyList);
        CountFilter = CopyCentralProxyList.Count;
    }
}