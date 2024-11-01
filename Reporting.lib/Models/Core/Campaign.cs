namespace Reporting.lib.Models.Core;

public class Campaign
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan Interval { get; set; } // Interval for recurring execution
    public int EmailGroupId { get; set; }
    public EmailGroup EmailGroup { get; set; }
    public ICollection<Process> Processes { get; set; } = new List<Process>();
}