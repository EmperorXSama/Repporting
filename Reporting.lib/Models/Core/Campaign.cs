namespace Reporting.lib.Models.Core;

public class Campaign : Process
{
    public TimeSpan Interval { get; set; }  // Interval for recurring execution
    public override bool IsCampaign { get; set; } = true;  // Override to identify as campaign
}
