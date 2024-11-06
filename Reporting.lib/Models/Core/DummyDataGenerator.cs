using Reporting.lib.enums.Core;

namespace Reporting.lib.Models.Core;

public static class DummyDataGenerator
{
     public static List<EmailAccount> EmailAccounts { get; private set; } = new List<EmailAccount>();
    public static List<Proxy> Proxies { get; private set; } = new List<Proxy>();
    public static List<EmailGroup> EmailGroups { get; private set; } = new List<EmailGroup>();
    public static List<Process> Processes { get; private set; } = new List<Process>();
    public static List<Campaign> Campaigns { get; private set; } = new List<Campaign>();

    static DummyDataGenerator()
    {
        GenerateProxies();
        GenerateEmailGroups();
        GenerateEmailAccounts();
        GenerateProcesses();
        GenerateCampaigns();
    }

    private static void GenerateProxies()
    {
        for (int i = 1; i <= 5; i++)
        {
            Proxies.Add(new Proxy
            {
                Id = i,
                ProxyIp = $"192.168.0.{i}",
                Port = 8080 + i,
                Username = $"proxyUser{i}",
                Password = $"proxyPass{i}"
            });
        }
    }

    private static void GenerateEmailGroups()
    {
        for (int i = 1; i <= 3; i++)
        {
            EmailGroups.Add(new EmailGroup
            {
                Id = i,
                Name = $"Group {i}"
            });
        }
    }

    private static void GenerateEmailAccounts()
    {
        for (int i = 1; i <= 10; i++)
        {
            EmailAccounts.Add(new EmailAccount
            {
                Id = i,
                EmailAddress = $"email{i}@example.com",
                Password = $"password{i}",
                RecoveryEmail = $"recovery{i}@example.com",
                Proxy = Proxies[i % Proxies.Count],
                Status = i % 2 == 0 ? EmailStatus.Active : EmailStatus.NewAdded,
                Group = EmailGroups[i % EmailGroups.Count],
                FirstUse = DateTime.Now.AddDays(-i),
                LastUse = DateTime.Now
            });
        }
    }

    private static void GenerateProcesses()
    {
        for (int i = 1; i <= 5; i++)
        {
            var process = new Process
            {
                Id = i,
                OperationName = $"Process {i}",
                StartTime = DateTime.Now.AddHours(-i),
                EmailGroup = EmailGroups[i % EmailGroups.Count]
            };

            // Add process logs for each email in the group
            foreach (var email in EmailAccounts.Where(e => e.Group.Id == process.EmailGroup.Id))
            {
                process.Logs.Add(new ProcessLog
                {
                    EmailAccount = email,
                    Process = process,
                    ProcessedAt = DateTime.Now,
                    Result = ProcessResult.Success,
                    SpamCount = i * 10
                });
            }

            Processes.Add(process);
        }
    }

    private static void GenerateCampaigns()
    {
        for (int i = 1; i <= 2; i++)
        {
            Campaigns.Add(new Campaign
            {
                Id = i,
                Name = $"Campaign {i}",
                StartTime = DateTime.Now.AddMinutes(i * 30),
                Interval = TimeSpan.FromHours(1),
                EmailGroup = EmailGroups[i % EmailGroups.Count],
                Processes = Processes.Take(2).ToList()
            });
        }
    }
    
}