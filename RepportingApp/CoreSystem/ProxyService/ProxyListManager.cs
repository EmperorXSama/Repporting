using System.Net;

namespace RepportingApp.CoreSystem.ProxyService;
using System.IO;
public static class ProxyListManager
{
    private const string GoogleUrl = "https://www.google.com";
    private const string YahooUrl = "https://www.yahoo.com";
    public static ConcurrentBag<Proxy> ReservedProxies { get; set; } = new ConcurrentBag<Proxy>();
    public static List<EmailAccount> DistributeEmailsBySubnetSingleList(List<EmailAccount> emailsGroupToWork)
    {
        // Group emails by subnet (first three octets)
        var groupedBySubnet = emailsGroupToWork
            .GroupBy(email => string.Join(".", email.Proxy.ProxyIp.Split('.').Take(3))) // Subnet based on the first 3 octets
            .OrderByDescending(group => group.Count()) // Larger groups first
            .ToList();

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

    public static void UploadReservedProxyFile()
    {
        // set path 
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var path = Path.Combine(desktop,"YahooEmails","Repporting","Files", "Proxy_list.txt");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The specified proxy file does not exist.", path);
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var parts = line.Split(':');
                if (parts.Length <4)
                {
                    continue;
                }

                var proxy = new Proxy
                {
                    ProxyIp = parts[0],
                    Port = int.Parse(parts[1]),
                    Username = parts[2],
                    Password = parts[3],
                };
                ReservedProxies.Add(proxy);
               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing line '{line}': {ex.Message}");
            }
        }
    }

    public static Proxy GetRandomDifferentSubnetProxy(Proxy proxy)
    {
        var currentSubnet = GetSubnet(proxy.ProxyIp);
        
        var random = new Random();
        var shuffledProxies = ReservedProxies.OrderBy(_ => random.Next()).ToList();

        foreach (var proxysh in shuffledProxies)
        {
            if (GetSubnet(proxysh.ProxyIp) != currentSubnet)
            {
                return proxysh;
            }
        }
        throw new InvalidOperationException("No proxy with a different subnet is available.");
    }
    public static string GetSubnet(string ipAddress)
    {
        var parts = ipAddress.Split('.');
        return parts.Length >= 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}" : string.Empty;
    }
    
    public static async Task TestProxiesAsync(CentralProxy proxy)
    {
        try
        {
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"{proxy.Ip}:{proxy.Port}")
                {
                    Credentials = new NetworkCredential(proxy.Username, proxy.Password)
                },
                UseProxy = true
            };

            using var httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(5) // Set timeout to avoid hanging
            };

            // Measure response time for Google connectivity
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            proxy.GoogleConnectivity = await GetStatusCodeAsync(httpClient, GoogleUrl);
            stopwatch.Stop();
            proxy.Ms = $"{stopwatch.ElapsedMilliseconds}ms";

            // Test Yahoo connectivity
            proxy.YahooConnectivity = await GetStatusCodeAsync(httpClient, YahooUrl);

            // Fetch region information
            proxy.Region = await GetRegionAsync(proxy.Ip);
        }
        catch
        {
            proxy.GoogleConnectivity = "Error";
            proxy.YahooConnectivity = "Error";
            proxy.Ms = "N/A";
            proxy.Region = "Unknown";
        }
    }
    private static async Task<string> GetRegionAsync(string ipAddress)
    {
        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            var response = await httpClient.GetStringAsync($"https://ipinfo.io/{ipAddress}/region");
            return response.Trim();
        }
        catch
        {
            return "Unknown";
        }
    }

    private static async Task<string> GetStatusCodeAsync(HttpClient httpClient, string url)
    {
        try
        {
            var response = await httpClient.GetAsync(url);
            return ((int)response.StatusCode).ToString(); // Return status code as string
        }
        catch
        {
            return "0"; 
        }
    }
}

