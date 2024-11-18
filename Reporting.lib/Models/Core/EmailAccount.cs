using System.Text.Json.Serialization;
using Reporting.lib.enums.Core;

namespace Reporting.lib.Models.Core;


public class EmailAccount
{
    public int Id { get; set; }

    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("recoveryEmail")]
    public string RecoveryEmail { get; set; }

    [JsonPropertyName("proxy")]
    public Proxy Proxy { get; set; }

    [JsonPropertyName("status")]
    public EmailStatus Status { get; set; }

    [JsonPropertyName("group")]
    public EmailGroup Group { get; set; }

    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";
    
    public EmailMetaData MetaIds { get; set; } = new();

    [JsonPropertyName("processLogs")]
    public ICollection<ProcessLog> ProcessLogs { get; set; } = new List<ProcessLog>();
    [JsonIgnore]

    public DateTime FirstUse => IdsFrequencies?.Any() == true ? IdsFrequencies.First().CollectedTime : DateTime.MinValue;
    [JsonIgnore]
    // Property to get the last collection time
    
    public DateTime LastUse => IdsFrequencies?.Any() == true ? IdsFrequencies.Last().CollectedTime : DateTime.MinValue;

    // Property for calculating ID lifespan in hours
    public double IdLifespan
    {
        get
        {
            var idCollectionTimes = IdsFrequencies
                .OrderByDescending(f => f.CollectedTime) // Sort times in descending order
                .Select(f => f.CollectedTime)
                .ToList();

            // We need at least two collected times to calculate the lifespan
            if (idCollectionTimes.Count >= 2)
            {
                var lastTime = idCollectionTimes[0];
                var preLastTime = idCollectionTimes[1];
                return Math.Round((lastTime - preLastTime).TotalHours); // Round to the nearest integer
            }

            return 0; // Return 0 if not enough data
        }
    }
    [JsonIgnore]
    public List<IdCollectionFrequency> IdsFrequencies { get; set; } = new List<IdCollectionFrequency>();
}
public class EmailStats
{
    public string ISP { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class IdCollectionFrequency()
{
    public int Id { get; set; }
    public string IdValue { get; set; }
    public DateTime CollectedTime { get; set; }
}