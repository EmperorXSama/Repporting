namespace RepportingApp.CoreSystem.ProxyService;
using System.IO;
public static class ProxyListManager
{


    public static List<EmailAccount> DistributeEmailsBySubnetSingleList(List<EmailAccount> emailsGroupToWork)
    {
        // Group emails by subnet (first three octets)
        var groupedBySubnet = emailsGroupToWork
            .GroupBy(email => string.Join(".", email.Proxy.ProxyIp.Split('.').Take(3))) // Subnet based on the first 3 octets
            .OrderByDescending(group => group.Count()) // Larger groups first
            .ToList();

        // Calculate unique subnets
        /*var uniqueSubnets = groupedBySubnet.Select(group => group.Key).ToList();*/

        // Write unique subnets to a text file on the desktop
        /*
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var filePath = Path.Combine(desktopPath, "UniqueSubnets.txt");
        File.WriteAllLines(filePath, uniqueSubnets);
        */

        // Shuffle emails inside each group to avoid patterns
        var shuffledGroups = groupedBySubnet
            .Select(group => group.OrderBy(_ => Guid.NewGuid()).ToList())
            .ToList();

        // Final distributed list using round-robin
        var distributedList = new List<EmailAccount>();
        int maxGroupSize = shuffledGroups.Max(g => g.Count);

        for (int i = 0; i < maxGroupSize; i++) // Distribute emails round-robin
        {
            foreach (var group in shuffledGroups)
            {
                if (i < group.Count)
                {
                    distributedList.Add(group[i]);
                }
            }
        }

        return distributedList;
    }


}