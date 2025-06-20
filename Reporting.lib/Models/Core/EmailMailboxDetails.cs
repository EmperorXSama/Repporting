using Newtonsoft.Json;

namespace Reporting.lib.Models.Core;

public class EmailMailboxDetails
{
    public string EmailAddress { get; set; }
    public string NickName { get; set; }
    public int? MailboxesCount { get; set; }

    public string ActiveAliasesRaw { get; set; }
    public string InactiveAliasesRaw { get; set; }
    public string PackAliasesRaw { get; set; }
    public int ActivePackNumber { get; set; }
    public string GroupName { get; set; }

    [JsonProperty("activeAliases")]
    public List<string> ActiveAliases { get; private set; }
    [JsonProperty("inactiveAliases")]
    public List<string> InactiveAliases { get; private set; }

    public Dictionary<int, List<string>> PackAliases { get; private set; } = new();

    public void ProcessAliases()
    {
        ActiveAliases = ActiveAliasesRaw?.Split(';').ToList() ?? new();
        InactiveAliases = InactiveAliasesRaw?.Split(';').ToList() ?? new();

        if (!string.IsNullOrWhiteSpace(PackAliasesRaw))
        {
            foreach (var packEntry in PackAliasesRaw.Split('|'))
            {
                var parts = packEntry.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0].Replace("Pack", ""), out int packNumber))
                {
                    var aliases = parts[1].Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                    PackAliases[packNumber] = aliases;
                }
            }
        }
    }

    public int TotalPacks { get; set; }
}

public class EmailAccountWithPackAliases
{
    public EmailAccount Account { get; set; }
    public List<string> PackAliasesToSwitch { get; set; }
}
