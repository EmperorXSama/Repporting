using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataAccess.Enums;
using DataAccess.Helpers.ExtensionMethods;
using DataAccess.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using RepportingApp.CoreSystem.Broadcast;
using RepportingApp.GeneralData;
using RepportingApp.ViewModels.Charts;
using SkiaSharp;


namespace RepportingApp.ViewModels;

public partial class DashboardPageViewModel : ViewModelBase
{


    #region main chart var

    private readonly ErrorProgressChart _errorProgressChart;
    public ObservableCollection<ISeries> Series => _errorProgressChart.Series;
    public ObservableCollection<Axis> XAxes => _errorProgressChart.XAxes;
    public ObservableCollection<Axis> YAxes => _errorProgressChart.YAxes;

    #endregion 
    #region spam chart var

    private readonly SpamCountChart _spamCountChart;
    public ObservableCollection<ISeries> SpamSeries => _spamCountChart.SpamSeries;
    public ObservableCollection<Axis> SpamXAxes => _spamCountChart.SpamXAxes;
    public ObservableCollection<Axis> SpamYAxes => _spamCountChart.SpamYAxes;

    #endregion
    #region isp chart var

    private readonly ISPChart _ispChart;
    public ObservableCollection<ISeries> PieSeries => _ispChart.PieSeries;

    #endregion

    #region Ui variables

    [ObservableProperty] private string _chartTitle = "Error progress/success over time";
    [ObservableProperty] private bool _isMenuOpen = false;
    [ObservableProperty] private bool _isPopupOpen = false;
    [ObservableProperty] private bool _isGridPopupOpen = false;
    [ObservableProperty] private bool _isCharOneVisible = true;
    [ObservableProperty] private string _searchLabel = "";
    [ObservableProperty] private int _totalEmails;
    [ObservableProperty] private double _idLifeSpan;
    [ObservableProperty] private bool _isChartTwoVisible = false;
    [ObservableProperty] private Status _selectedStatus;
    [ObservableProperty] private ProxyStat _selectedByProxy;
    [ObservableProperty] private ObservableCollection<Status> statuses;  
    [ObservableProperty] private ObservableCollection<ProxyStat> _byProxy;
    [ObservableProperty]
    private int _selectedTimePeriod;
    
    [ObservableProperty]
    private ObservableCollection<EmailsCoreModel> _networkItems;
    [ObservableProperty]
    private ObservableCollection<EmailsCoreModel> _emailDiaplysTable;

    #endregion
    

    #region Services

    private readonly IMessenger _messanger;
    public ToastNotificationViewModel ToastNotificationViewModel { get; set; } = new ToastNotificationViewModel();
    
   
    #endregion
    public ObservableCollection<RegionProxyInfo> RegionProxyInfoList { get; set; }

    public DashboardPageViewModel(IMessenger messenger):base(messenger)
    {
        _messanger = messenger;
        #region main chart
        _errorProgressChart = new ErrorProgressChart();
        _errorProgressChart.GenerateChartData();
        #endregion
        #region Spam Chart
        _spamCountChart = new SpamCountChart();
        _spamCountChart.GenerateSpamChartData();
        #endregion   
        #region Spam Chart
        _ispChart = new ISPChart();
        _ispChart.LoadEmailData();
        #endregion
        
       
        var proxies =ProxyData.GetSampleProxyData();
        var regionInfo = GetRegionProxyInfo(proxies);
        RegionProxyInfoList = new ObservableCollection<RegionProxyInfo>(regionInfo);
        Statuses = new ObservableCollection<Status>
        {
            Status.Active,
            Status.New,
            Status.Old,
            Status.Blocked
        }; 
        ByProxy = new ObservableCollection<ProxyStat>
        {
            ProxyStat.NA,
            ProxyStat.Duplicated,
        };
        NetworkItems = EmailCoreData.GetEmailCoreData();
        EmailDiaplysTable = NetworkItems;
        CountFilter = EmailDiaplysTable.Count();
        TotalEmails = NetworkItems.Count();
        IdLifeSpan = Logic.GetAverageIdLifespan(NetworkItems);
    }



    #region Relay commands

    
    [RelayCommand]
    private void ToggleChart()
    {
        if (ChartTitle.Contains("Error progress"))
        {
            ChartTitle = "Spam Count Over Time";
        }
        else
        {
            ChartTitle = "Error progress/success over time";
        }
        IsCharOneVisible = !IsCharOneVisible;
        IsChartTwoVisible = !IsChartTwoVisible;
    }

   
    [RelayCommand]
    private void ToggleSidePanel()
    {
        IsMenuOpen = !IsMenuOpen;
    }
    
    


    [RelayCommand]
    private void TogglePopup()
    {
        IsPopupOpen = !IsPopupOpen;
    } 
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

        // Log before sending
        Debug.WriteLine("Sending ProcessStartMessage...");
        
        _messanger.Send(new ProcessStartMessage("Filtering Data", new { param1 = "value1" }));
        await ShowToast();
        await Task.Delay(10000);

        // Log before sending the finish message
        Debug.WriteLine("Sending ProcessFinishedMessages...");

        _messanger.Send(new ProcessFinishedMessages("ProcessName", new { Result1 = "Success" }));
        var sd = SelectedDate;
        var st = SelectedTime;

        // Base query from original data
        var filteredList = NetworkItems.AsQueryable();

        // Combine selected date and time if both are set
        DateTime? selectedDateTime = null;
        if (sd.HasValue && st.HasValue)
        {
            selectedDateTime = sd.Value.Date + st.Value; // Combine the date and time
        }

        // Apply status filtering
        if (SelectedStatus != Status.None)
        {
            filteredList = filteredList.Where(item => item.Status == SelectedStatus);
        }

        // Apply proxy status filtering (for Duplicated proxy scenario)
        if (SelectedByProxy == ProxyStat.Duplicated)
        {
            filteredList = filteredList
                .GroupBy(item => item.Proxy) // Group by proxy
                .Where(g => g.Count() > 1)   // Find duplicates
                .SelectMany(g => g);         // Flatten the result
        }

        if (!string.IsNullOrWhiteSpace(SearchLabel))
        {
            filteredList = filteredList.Where(e => e.EmailAddress.Contains(SearchLabel));
        }

        // Apply date filtering based on LastUse
        if (selectedDateTime.HasValue)
        {
            if (IsBeforeChecked && !IsAfterChecked)
            {
                filteredList = filteredList.Where(item => item.LastUse <= selectedDateTime.Value);
            }
            else if (!IsBeforeChecked && IsAfterChecked)
            {
                filteredList = filteredList.Where(item => item.LastUse >= selectedDateTime.Value);
            }
        }

        // Update the display table
        EmailDiaplysTable = new ObservableCollection<EmailsCoreModel>(filteredList);
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

private List<RegionProxyInfo> GetRegionProxyInfo(List<ProxyModel> proxies)
{
    var totalProxies = proxies.Count;
    var regionCounts = proxies
        .GroupBy(p => p.Region)
        .Select(g => new RegionProxyInfo
        {
            Region = g.Key,
            Count = g.Count(),
            Percentage = (g.Count() / (double)totalProxies) * 100
        })
        .OrderByDescending(r => r.Count)
        .ToList();

    return regionCounts;
}

public async Task ShowToast()
{
    await ToastNotificationViewModel.ShowToast("This is a toast message!","Success");
}






}

