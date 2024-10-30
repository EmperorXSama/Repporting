

namespace RepportingApp.Models;

public class StartProcessNotifierModel
{
    public ReportingSittingsProcesses ReportingSettingsP { get; set; }
    public int Thread { get; set; }
    public int Repetition { get; set; }
    public int RepetitionDelay { get; set; }
    public ProxySittings SelectedProxySetting;
    public ReportingSettings SelectedReportSetting;
    public DateTime ProcessDate { get; set; } = DateTime.UtcNow;
}