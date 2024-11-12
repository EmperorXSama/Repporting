namespace Reporting.lib.Models.Core;

public class ProcessSettingsModel
{
    public int Id { get; set; }
    public int ProcessId { get; set; }
    public StartProcessNotifierModel Settings { get; set; }
}