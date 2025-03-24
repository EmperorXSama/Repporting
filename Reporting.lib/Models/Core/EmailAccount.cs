

namespace Reporting.lib.Models.Core;

public partial class EmailAccount : ObservableObject
{
    #region core info 

    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("recoveryEmail")]
    public string RecoveryEmail { get; set; }

    [JsonPropertyName("proxy")]
    public Proxy? Proxy { get; set; }

    #endregion

    public int Id { get; set; }

    [JsonPropertyName("status")]
    public EmailStatus Status { get; set; }

    [JsonPropertyName("group")]
    public EmailGroup Group { get; set; }
    
    public EmailAccountStats? Stats { get; set; }

    [JsonPropertyName("UserAgent")]
    public string UserAgent { get; set; }
    public EmailMetaData? MetaIds { get; set; } = new();
    [ObservableProperty]
    private ObservableCollection<KeyValuePair<string, object>> apiResponses = new();
    
        [ObservableProperty]
    private ObservableCollection<object> _extraData = new();
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