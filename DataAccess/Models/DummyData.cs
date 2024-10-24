namespace DataAccess.Models;

public static class DummyData
{
    // Public static properties to hold the data
    public static List<ProcessModel> Processes { get; private set; }
    public static List<EmailsCoreModel> GetEmailsList()
    {
        return new List<EmailsCoreModel>
        {
            new EmailsCoreModel { Id = 1, EmailAddress = "user1@gmail.com", Password = "pass", GroupId = 1 },
            new EmailsCoreModel { Id = 2, EmailAddress = "user2@gmail.com", Password = "pass", GroupId = 1 },
            new EmailsCoreModel { Id = 3, EmailAddress = "user3@yahoo.com", Password = "pass", GroupId = 2 },
            new EmailsCoreModel { Id = 4, EmailAddress = "user4@yahoo.com", Password = "pass", GroupId = 2 },
            new EmailsCoreModel { Id = 5, EmailAddress = "user5@att.net", Password = "pass", GroupId = 3 },
            // Add more emails
        };
    }
 public static void LoadDummyData()
{
    Processes = new List<ProcessModel>
{
    new ProcessModel
    {
        OperationName = "Reporting",
        NumberOfEmailsInProcess = 30,
        SuccessCount = 15,
        ProxyErrorCount = 6,
        HttpErrorCount = 3,
        ProcessDate = DateTime.Today.AddDays(-7), // A week ago
        OthersCount = 6,
        SpamCounts = new List<SpamCountModel>
        {
            new SpamCountModel { Email = "email1@example.com", SpamCount = 120, CountDate = DateTime.Today.AddDays(-7) },
            new SpamCountModel { Email = "email2@example.com", SpamCount = 20, CountDate = DateTime.Today.AddDays(-7) }
        }
    },
    new ProcessModel
    {
        OperationName = "Reporting",
        NumberOfEmailsInProcess = 13,
        SuccessCount = 9,
        ProxyErrorCount = 4,
        HttpErrorCount = 2,
        ProcessDate = DateTime.Today.AddDays(-5), // 5 days ago
        OthersCount = 2,
        SpamCounts = new List<SpamCountModel>
        {
            new SpamCountModel { Email = "email1@example.com", SpamCount = 30, CountDate = DateTime.Today.AddDays(-5) },
            new SpamCountModel { Email = "email2@example.com", SpamCount = 10, CountDate = DateTime.Today.AddDays(-5) }
        }
    },
    new ProcessModel
    {
        OperationName = "Reporting",
        NumberOfEmailsInProcess = 130,
        SuccessCount = 115,
        ProxyErrorCount = 8,
        HttpErrorCount = 5,
        ProcessDate = DateTime.Today.AddDays(-3), // 3 days ago
        OthersCount = 2,
        SpamCounts = new List<SpamCountModel>
        {
            new SpamCountModel { Email = "email1@example.com", SpamCount = 60, CountDate = DateTime.Today.AddDays(-3) },
            new SpamCountModel { Email = "email2@example.com", SpamCount = 40, CountDate = DateTime.Today.AddDays(-3) }
        }
    },
    new ProcessModel
    {
        OperationName = "Collect IDs",
        NumberOfEmailsInProcess = 30,
        SuccessCount = 18,
        ProxyErrorCount = 4,
        HttpErrorCount = 5,
        ProcessDate = DateTime.Today.AddDays(-2), // 2 days ago
        OthersCount = 3
        // No spam counts for non-reporting processes
    },
    new ProcessModel
    {
        OperationName = "Change Password",
        NumberOfEmailsInProcess = 30,
        SuccessCount = 20,
        ProxyErrorCount = 3,
        HttpErrorCount = 5,
        ProcessDate = DateTime.Today.AddDays(-1), // Yesterday
        OthersCount = 2
        // No spam counts for non-reporting processes
    },
    new ProcessModel
    {
        OperationName = "Change Password",
        NumberOfEmailsInProcess = 35,
        SuccessCount = 25,
        ProxyErrorCount = 5,
        HttpErrorCount = 4,
        ProcessDate = DateTime.Today, // Today
        OthersCount = 1
        // No spam counts for non-reporting processes
    }
};

}
public static List<EmailStats> GetEmailStats(List<EmailsCoreModel> emails)
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
}


