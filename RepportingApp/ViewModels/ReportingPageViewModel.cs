

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepportingApp.ViewModels;

public partial class ReportingPageViewModel : ViewModelBase
{

    #region UI
    [ObservableProperty] private bool _isMenuOpen = false;
    [ObservableProperty] private bool _isPopupOpen = false;

    [ObservableProperty] private bool _isFixed;
    [ObservableProperty] private bool _isRandom; 
    [ObservableProperty] private bool _isOneByOne;
    [ObservableProperty] private bool _isAll;
    [ObservableProperty] private ObservableCollection<string> _reportingselectedProcessesToIcon = new ObservableCollection<string>();
   
    #endregion

    #region Logic
    [ObservableProperty] private ReportingSettingValues _reportingSettingsValuesDisplay;
    [ObservableProperty]private ProxySittings _selectedProxySetting;
    [ObservableProperty]private ReportingSettings _selectedReportSetting;
    [ObservableProperty] private ReportingSittingsProcesses _selectedProcesses = new ReportingSittingsProcesses();
    #endregion
   
    

 

    public ReportingPageViewModel(IMessenger messenger) : base(messenger)
    {
        ReportingSettingsValuesDisplay = new ReportingSettingValues(App.Configuration);
        
    }


    #region Relay commands

    [RelayCommand]
    private async Task StartOperation()
    {
        var modifier = new StartProcessNotifierModel()
        {
            ReportingSettingsP = SelectedProcesses,
            Thread = ReportingSettingsValuesDisplay.Thread,
            Repetition = ReportingSettingsValuesDisplay.Repetition,
            RepetitionDelay = ReportingSettingsValuesDisplay.RepetitionDelay,
            SelectedProxySetting =  this.SelectedProxySetting,
            SelectedReportSetting =  this.SelectedReportSetting,
        };
        _messenger.Send(new ProcessStartMessage("Success","Reporting",modifier));
    }
    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        await ReportingSettingsValuesDisplay.SaveConfigurationAsync();
    }
   [RelayCommand] 
    private void ProcessesCheckedManager()
    {
        // Define a mapping between the process selection and the corresponding icons
        var processIconMapping = new Dictionary<Func<ReportingSittingsProcesses, bool>, string>
        {
            { p => p.IsReportingSelected, "ReportingIcon" },
            { p => p.IsDeleteSpamSelected, "DeleteSpamIcon" },
            { p => p.IsGetSpamNumbersSelected, "CollectSpamNumberIcon" },
            { p => p.IsGetSpamSubjectSelected, "CollectSpamSubjectIcon" },
            { p => p.IsFixAfterFinish, "FixIcon" },
            
        };

        // Iterate through the mapping
        foreach (var mapping in processIconMapping)
        {
            var isSelected = mapping.Key(SelectedProcesses);  // Use _selectedProcesses instead of SelectedProcesses
            var iconName = mapping.Value;

            if (isSelected)
            {
                // Add the icon if it is selected
                if (!ReportingselectedProcessesToIcon.Contains(iconName))  // Corrected the collection reference
                {
                    ReportingselectedProcessesToIcon.Add(iconName);
                }
            }
            else
            {
                // Remove the icon if it is not selected
                if (ReportingselectedProcessesToIcon.Contains(iconName))  // Corrected the collection reference
                {
                    ReportingselectedProcessesToIcon.Remove(iconName);
                }
            }
        }
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
        }else if (IsRandom)
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
        }else if (IsAll)
        {
            SelectedReportSetting =  ReportingSettings.All;
        }
    }
    [RelayCommand] private void OpenPopup()  => IsPopupOpen = true;
    [RelayCommand] private void ClosePopup()  => IsPopupOpen = false;
    #endregion
    

}
