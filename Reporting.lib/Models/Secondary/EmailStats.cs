using CommunityToolkit.Mvvm.ComponentModel;

namespace Reporting.lib.Models.Secondary;

public class EmailAccountStats
{
    public int InboxCount { get; set; }
    public int SpamCount { get; set; }
    public int LastReadCount { get; set; }
    public int LastArchivedCount { get; set; }
    public int LastNotSpamCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}

