using DataAccess.Enums;

namespace DataAccess.Models;

public class EmailsCoreModel
{
    public int Id { get; set; }
    public string EmailAddress { get; set; }
    public string Password { get; set; }
    public int GroupId { get; set; } // Foreign key to Group
    public GroupModel Group { get; set; } // Navigation property
    public string MailBox { get; set; }
    public string Proxy { get; set; }
    public string Port { get; set; }
    public int NumSpam { get; set; }
    public Status Status { get; set; }

    // Property to get the first collection time
    public DateTime FirstUse => IdsFrequencies?.Any() == true ? IdsFrequencies.First().CollectedTime : DateTime.MinValue;

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


    // List to store ID collection frequencies
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
    public DateTime CollectedTime { get; set; }
}

