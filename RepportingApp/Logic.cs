using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DataAccess.Models;
using LiveChartsCore.Defaults;

namespace RepportingApp;

public static class Logic
{
    public static double GetAverageIdLifespan(ObservableCollection<EmailAccount> networkItems)
    {
        List<double> lifespans = new List<double>();

        foreach (var email in networkItems)
        {
            var idCollectionTimes = email.IdsFrequencies
                .OrderByDescending(f => f.CollectedTime)
                .Select(f => f.CollectedTime)
                .ToList();

            // We need at least two collected times to calculate the lifespan
            if (idCollectionTimes.Count >= 2)
            {
                // Calculate lifespan as the difference between the last two collection times
                var lastTime = idCollectionTimes[0];
                var preLastTime = idCollectionTimes[1];
                var lifespan = (lastTime - preLastTime).TotalHours; // Convert to hours

                // Only add if lifespan is non-negative
                if (lifespan > 0)
                {
                    lifespans.Add(lifespan);
                }
                else
                {
                    Console.WriteLine($"Negative lifespan for email {email.EmailAddress}: {lifespan} hours.");
                }
            }
        }

        // Calculate the average lifespan
        return Math.Round(lifespans.Any() ? lifespans.Average() : 0); // Return 0 if no lifespans were calculated
    }


 

}