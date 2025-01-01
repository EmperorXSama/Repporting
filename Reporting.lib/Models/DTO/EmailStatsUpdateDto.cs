namespace Reporting.lib.Models.DTO;

public class EmailStatsUpdateDto
{
    public int EmailAccountId { get; set; }
    public int InboxCount { get; set; }
    public int SpamCount { get; set; }
    public int LastReadCount { get; set; }
    public int LastArchivedCount { get; set; }
    public int LastNotSpamCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}