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
    
    private readonly HttpClient HttpClient = new();
    private readonly ConcurrentDictionary<string, string?> RegionCache = new(); // Cache for IP lookups
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _subnetLocks = new();
    private readonly int MaxConcurrentPerSubnet = 5;
    
    public async Task<List<Proxy>> TestProxiesAsync(List<Proxy> proxies, int maxRetries = 3, bool force = true)
    {
        var batchedProxies = proxies.Chunk(100); // Can be adjusted dynamically if needed
        List<Task> allTasks = new List<Task>();

        foreach (var batch in batchedProxies)
        {
            var tasks = batch.Select(TestProxyWithSubnetControlAsync);
            allTasks.AddRange(tasks);
        }

        await Task.WhenAll(allTasks); // Run all tasks in parallel

        // Fetch regions for all proxies in one go
        await FetchRegionsInBatchAsync(proxies);

        if (force)
        {
            InvokeOnProxiesUpdated(proxies);
        }

        return proxies;
    }

    public  event EventHandler<List<Proxy>> OnProxiesUpdated;

    private  void InvokeOnProxiesUpdated(List<Proxy> proxies)
    {
        OnProxiesUpdated?.Invoke(this, proxies);
    }

    // Helper method to test individual proxy connectivity
    // Test a proxy's connectivity
    private async Task<bool> TestProxyAsync(Proxy proxy)
    {
        try
        {
            using var httpClientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"{proxy.ProxyIp}:{proxy.Port}")
                {
                    Credentials = new NetworkCredential(proxy.Username, proxy.Password)
                },
                UseProxy = true
            };

            using var httpClient = new HttpClient(httpClientHandler) { Timeout = TimeSpan.FromSeconds(5) };

            return await TestConnectivityAsync(httpClient, "https://www.yahoo.com");
        }
        catch
        {
            return false;
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
    private async Task FetchRegionsInBatchAsync(List<Proxy> proxies)
    {
        var ipsToFetch = proxies.Select(p => p.ProxyIp).Distinct()
            .Where(ip => !RegionCache.TryGetValue(ip, out string? region) || string.IsNullOrEmpty(region))
            .ToList();

        if (ipsToFetch.Count == 0) return;

        int batchSize = 100; // API supports large batch requests
        var batchedIps = ipsToFetch.Chunk(batchSize);

        List<Task> fetchTasks = new List<Task>();

        foreach (var batch in batchedIps)
        {
            fetchTasks.Add(FetchRegionsAsync(batch.ToList()));
        }

        await Task.WhenAll(fetchTasks);

        // Assign regions to proxies
        foreach (var proxy in proxies)
        {
            if (RegionCache.TryGetValue(proxy.ProxyIp, out string region))
            {
                proxy.Region = region;
            }
        }
    }

// Fetch regions in batches (parallel requests)
    private async Task FetchRegionsAsync(List<string> ipBatch)
    {
        try
        {
            string url = "http://ip-api.com/batch?key=oOVecJdhB0IK8p4";
            string jsonRequest = System.Text.Json.JsonSerializer.Serialize(ipBatch);

            using var requestContent = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
            using var response = await HttpClient.PostAsync(url, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                await Task.Delay(5000); // Wait 5 seconds and retry once
                var response1 = await HttpClient.PostAsync(url, requestContent);
                if (!response1.IsSuccessStatusCode) return; // If still failing, exit
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

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
    }

    
    private async Task TestProxyWithSubnetControlAsync(Proxy proxy)
    {
        string subnet = GetSubnet2(proxy.ProxyIp);

        // Get or create the lock for this subnet
        var subnetLock = _subnetLocks.GetOrAdd(subnet, _ => new SemaphoreSlim(MaxConcurrentPerSubnet, MaxConcurrentPerSubnet));

        // Wait for an available slot in this subnet
        await subnetLock.WaitAsync();

        try
        {
            bool success = await TestProxyWithRetriesAsync(proxy, 3);
            proxy.YahooConnectivity = success ? "Working" : "Failed";
        }
        finally
        {
            subnetLock.Release();
        }

        // Add a small delay to prevent detection (randomized for natural behavior)
        await Task.Delay(Random.Shared.Next(2000, 6000));
    }


    private async Task<bool> TestProxyWithRetriesAsync(Proxy proxy, int retries)
    {
        for (int i = 0; i < retries; i++)
        {
            if (await TestProxyAsync(proxy))
                return true;

            await Task.Delay(2000 * (i + 1)); // Exponential backoff
        }

        return false;
    }
    public static string GetSubnet2(string ip)
    {
        var parts = ip.Split('.');
        return $"{parts[0]}.{parts[1]}.{parts[2]}"; // First three octets define the subnet
    }

    // Helper class to deserialize the API response
    private class RegionResponse
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

}

public class UpdateProxiesEventArgs : EventArgs
{
    public List<Proxy> List { get; set; }
}