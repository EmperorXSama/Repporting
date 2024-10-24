using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataAccess.Enums;
using DataAccess.Helpers.ExtensionMethods;
using DataAccess.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
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

    #region Ui variables

    [ObservableProperty] private string _chartTitle = "Error progress/success over time";

    #endregion
    
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
    private ObservableCollection<ISeries> pieSeries;
    [ObservableProperty]
    private int _selectedTimePeriod;
    
    [ObservableProperty]
    private ObservableCollection<EmailsCoreModel> _networkItems;
    [ObservableProperty]
    private ObservableCollection<EmailsCoreModel> _emailDiaplysTable;

    #region main error flow chart 
    
  

    #endregion
    public ObservableCollection<RegionProxyInfo> RegionProxyInfoList { get; set; }

    public DashboardPageViewModel()
    {
        #region main chart
        _errorProgressChart = new ErrorProgressChart();
        _errorProgressChart.GenerateChartData();
        #endregion
        #region Spam Chart
        _spamCountChart = new SpamCountChart();
        _spamCountChart.GenerateSpamChartData();
        #endregion
        PieSeries = new ObservableCollection<ISeries>();
        var proxies =ProxyData.GetSampleProxyData();
        var regionInfo = GetRegionProxyInfo(proxies);
        RegionProxyInfoList = new ObservableCollection<RegionProxyInfo>(regionInfo);
        LoadEmailData();
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
    
   private void LoadEmailData()
{
    var emails = GetEmailsList();
    var stats = GetEmailStats(emails);
    

    // Sum up the total count for calculating angles
    var totalCount = stats.Sum(s => s.Count);

    double cumulativeAngle = 0;

    foreach (var stat in stats)
    {
        var percentage = stat.Count / (double)totalCount * 100;
        var angle = cumulativeAngle + (percentage / 100 * 360) / 2; // Mid-angle of the slice

        cumulativeAngle += percentage / 100 * 360; // Increment the cumulative angle
    }

    PieSeries = new ObservableCollection<ISeries>
    {
        new PieSeries<double>
        {
            MaxRadialColumnWidth = 60,
            Values = new double[] { stats.FirstOrDefault(s => s.ISP == "gmail.com")?.Count ?? 0 },
            Name = "Gmail",
            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
            DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#E5E6E4")),
            DataLabelsSize = 24,
            Pushout = 1,
            Stroke = new SolidColorPaint(SKColor.Parse("#A6A2A2")),
            Fill = new SolidColorPaint(SKColor.Parse("#A6A2A2")),
        },
        new PieSeries<double>
        {
            MaxRadialColumnWidth = 60,
            Values = new double[] { stats.FirstOrDefault(s => s.ISP == "yahoo.com")?.Count ?? 0 },
            Name = "Yahoo",
            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
            DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#847577")),
            DataLabelsSize = 24,
            Pushout = 1,
            Stroke = new SolidColorPaint(SKColor.Parse("#A6A2A2")),
            Fill = new SolidColorPaint(SKColor.Parse("#CFD2CD")),
        },
        new PieSeries<double>
        {
            MaxRadialColumnWidth = 60,
            Values = new double[] { stats.FirstOrDefault(s => s.ISP == "att.net")?.Count ?? 0 },
            Name = "ATT",
            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
            DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#CFD2CD")),
            DataLabelsSize = 24,
            Pushout = 1,
            Stroke = new SolidColorPaint(SKColor.Parse("#A6A2A2")),
            Fill = new SolidColorPaint(SKColor.Parse("#847577")),
        }
    };
}

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
    private List<EmailsCoreModel> GetEmailsList()
    {
        return new List<EmailsCoreModel>
        {
            new EmailsCoreModel { Id = 1, EmailAddress = "user1@gmail.com", Password = "pass", GroupId = 1 },
            new EmailsCoreModel { Id = 2, EmailAddress = "user2@gmail.com", Password = "pass", GroupId = 1 },
            new EmailsCoreModel { Id = 3, EmailAddress = "user3@yahoo.com", Password = "pass", GroupId = 2 },
            new EmailsCoreModel { Id = 4, EmailAddress = "user4@yahoo.com", Password = "pass", GroupId = 2 },
            new EmailsCoreModel { Id = 5, EmailAddress = "user5@att.net", Password = "pass", GroupId = 3 },
            new EmailsCoreModel { Id = 6, EmailAddress = "user6@gmail.com", Password = "pass", GroupId = 1 }
            // Add more emails if necessary
        };
    }
    private List<EmailStats> GetEmailStats(List<EmailsCoreModel> emails)
    {
        var totalEmails = emails.Count;
        var ispGroups = emails.GroupBy(email => email.EmailAddress.Split('@')[1]);

        var stats = ispGroups.Select(group => new EmailStats
        {
            ISP = group.Key,
            Count = group.Count(),
            Percentage = (group.Count() / (double)totalEmails) * 100
        }).ToList();

        return stats;
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

   



[ObservableProperty]
private bool isBeforeChecked;
[ObservableProperty]
private int _countFilter;

[ObservableProperty]
private bool isAfterChecked;
[ObservableProperty]
private DateTimeOffset? selectedDate = DateTimeOffset.UtcNow; // Nullable to allow for no selection.

[ObservableProperty]
private TimeSpan? selectedTime = DateTime.UtcNow.TimeOfDay; // Nullable to allow for no selection.
[RelayCommand]
private void FilterTable()
{
    var s = SelectedDate;
    var v = SelectedTime;

    // Base query from original data
    var filteredList = NetworkItems.AsQueryable();

    // Combine selected date and time if both are set
    DateTime? selectedDateTime = null;
    if (s.HasValue && v.HasValue)
    {
        selectedDateTime = s.Value.Date + v.Value; // Combine the date and time
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


}

