using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace RepportingApp.ViewModels;

public partial class HomePageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _currentHour;

    [ObservableProperty]
    private string _currentYear;

    [ObservableProperty]
    private string _currentMonthDay;




    public HomePageViewModel(IMessenger messenger) : base(messenger)
    {
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += UpdateTime;
        timer.Start();
    }



    private void UpdateTime(object sender, EventArgs eventArgs)
    {
        TimeZoneInfo moroccoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Morocco Standard Time");
        DateTime moroccoTime = TimeZoneInfo.ConvertTime(DateTime.Now, moroccoTimeZone);

        CurrentHour = moroccoTime.ToString("HH:mm:ss");
        CurrentYear = moroccoTime.Year.ToString();
        CurrentMonthDay = moroccoTime.ToString("MM/dd");

    }
}