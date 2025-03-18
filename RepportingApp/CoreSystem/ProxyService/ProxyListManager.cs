using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reporting.lib.Models.DTO;

namespace RepportingApp.CoreSystem.ProxyService;
using System.IO;

public  class ProxyListManager
{
    public static  ConcurrentBag<Proxy> ReservedProxies { get; set; } = new ConcurrentBag<Proxy>();
    public  static ConcurrentBag<Proxy> DbProxy { get; set; } = new ConcurrentBag<Proxy>();

    public  List<EmailAccount> DistributeEmailsBySubnetSingleList(List<EmailAccount> emailsGroupToWork)
    {
        // Group emails by subnet (first three octets)
        var groupedBySubnet = emailsGroupToWork
            .GroupBy(email =>
                string.Join(".", email.Proxy.ProxyIp.Split('.').Take(3))) // Subnet based on the first 3 octets
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

    public  void UploadReservedProxyFile()
    {
        ReservedProxies.Clear();
        // set path 
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var path = Path.Combine(desktop, "YahooEmails", "Repporting", "Files", "Proxy_list.txt");
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
                if (parts.Length < 4)
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

    public  void GetDBProxies(IEnumerable<Proxy> proxies)
    {
        DbProxy = new ConcurrentBag<Proxy>(); // Clear existing data if necessary

        foreach (var proxy in proxies)
        {
            DbProxy.Add(proxy);
        }
    }

public  async Task<List<Proxy>> UploadNewProxyFileAsync()
    {
        var newProxies = new ConcurrentBag<Proxy>();
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var path = Path.Combine(desktop, "YahooEmails", "Repporting", "Files", "WebShareProxy_list.txt");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The specified proxy file does not exist.", path);
        }

        await Task.Run(() =>
        {
            var lines = File.ReadLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            int batchSize = 1000;

            Parallel.ForEach(Partitioner.Create(0, lines.Count, batchSize), range =>
            {
                var localList = new List<Proxy>();

                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var parts = lines[i].Split(':');
                    if (parts.Length < 4) continue;

                    try
                    {
                        var proxy = new Proxy
                        {
                            ProxyIp = parts[0],
                            Port = int.Parse(parts[1]),
                            Username = parts[2],
                            Password = parts[3],
                        };

                        localList.Add(proxy);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing line '{lines[i]}': {ex.Message}");
                    }
                }

                foreach (var proxy in localList)
                {
                    newProxies.Add(proxy);
                }
            });
        });
        
        return newProxies.ToList();
    }
    public  Proxy GetRandomDifferentSubnetProxy(Proxy proxy)
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
    public  Proxy GetRandomDifferentSubnetProxyDb(Proxy proxy)
    {
        var currentSubnet = GetSubnet(proxy.ProxyIp);
        
        var random = new Random();
        var shuffledProxies = DbProxy.OrderBy(_ => random.Next()).ToList();

        foreach (var proxysh in shuffledProxies)
        {
            if (GetSubnet(proxysh.ProxyIp) != currentSubnet)
            {
                return proxysh;
            }
        }
        throw new InvalidOperationException("No proxy with a different subnet is available.");
    }
    public  string GetSubnet(string ipAddress)
    {
        var parts = ipAddress.Split('.');
        return parts.Length >= 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}" : string.Empty;
    }
    
    private  readonly SemaphoreSlim Semaphore = new(100); // Controls concurrency (10 at a time)
    private  readonly HttpClient HttpClient = new();
    private  readonly ConcurrentDictionary<string, string> RegionCache = new(); // Cache for IP lookups

    // Main method for testing proxies
    public  async Task<List<Proxy>> TestProxiesAsync(List<Proxy> proxies)
    {
        var batchedProxies = proxies.Chunk(100); // Process in chunks of 100

        foreach (var batch in batchedProxies)
        {
            await Parallel.ForEachAsync(batch, new ParallelOptions { MaxDegreeOfParallelism = 100 }, async (proxy, _) =>
            {
                await Semaphore.WaitAsync();
                try
                {
                    await TestProxyAsync(proxy);
                }
                finally
                {
                    Semaphore.Release();
                }
            });

            // Fetch regions for the tested batch
            await FetchRegionsInBatchAsync(batch.ToList());
            InvokeOnProxiesUpdated(batch.ToList());
         
        }

        return proxies;
    }

    public  event EventHandler<List<Proxy>> OnProxiesUpdated;

    private  void InvokeOnProxiesUpdated(List<Proxy> proxies)
    {
        OnProxiesUpdated?.Invoke(this, proxies);
    }

    // Helper method to test individual proxy connectivity
    private  async Task TestProxyAsync(Proxy proxy)
    {
        try
        {
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"{proxy.ProxyIp}:{proxy.Port}")
                {
                    Credentials = new NetworkCredential(proxy.Username, proxy.Password)
                },
                UseProxy = true
            };

            using var httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            

            // Test Yahoo connectivity
            bool yahooSuccess = await TestConnectivityAsync(httpClient, "https://www.yahoo.com");
            proxy.YahooConnectivity = yahooSuccess ? "Working" : "Failed";
        }
        catch
        {
            proxy.YahooConnectivity = "Error";
            proxy.Region = "Unknown";
            proxy.Availability = false;
        }
    }

    // Helper method for testing connectivity to a specific URL
    private  async Task<bool> TestConnectivityAsync(HttpClient httpClient, string url)
    {
        try
        {
            using var response = await httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Fetch region information for proxies in batches
    private  async Task FetchRegionsInBatchAsync(List<Proxy> proxies)
    {
        var ipsToFetch = proxies.Select(p => p.ProxyIp).Distinct().ToList();

        try
        {
            string url = "http://ip-api.com/batch?key=oOVecJdhB0IK8p4";
            string jsonRequest = System.Text.Json.JsonSerializer.Serialize(ipsToFetch);

            using var requestContent = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
            using var response = await HttpClient.PostAsync(url, requestContent);

            if (!response.IsSuccessStatusCode) return;

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var results = System.Text.Json.JsonSerializer.Deserialize<List<RegionResponse>>(jsonResponse, options);


            if (results == null) return;

            foreach (var result in results)
            {
                if (result.Status == "success" && !string.IsNullOrEmpty(result.Country))
                {
                    RegionCache[result.Query] = result.Country;
                }
            }

        }
        catch
        {
            // Ignore failed request
        }

        foreach (var proxy in proxies)
        {
            if (RegionCache.TryGetValue(proxy.ProxyIp, out string region))
            {
                proxy.Region = region;
            }
        }
    }

    // Helper class to deserialize the API response
    private class RegionResponse
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

}

public class UpdateProxiesEventArgs : EventArgs
{
    public List<Proxy> List { get; set; }
}