using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace RepportingApp.CoreSystem.ApiSystem;

public class UnifiedApiClient : IApiConnector
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, ProxyState> _proxyStates = new();
    private readonly int _maxRequestsPerProxy = 3;
    private readonly ICacheService _cacheService;
    private static readonly SemaphoreSlim _semaphore = new(10);
    private bool IsJson(string input)
    {
        input = input.Trim();
        return (input.StartsWith("{") && input.EndsWith("}")) || 
               (input.StartsWith("[") && input.EndsWith("]"));
    }
    public UnifiedApiClient(ICacheService cacheService)
    {
        _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });
        _cacheService = cacheService;
    }

    private class ProxyState
    {
        public HttpClient Client { get; }
        public SemaphoreSlim Semaphore { get; }

        public ProxyState(HttpClient client, int maxRequests)
        {
            Client = client;
            Semaphore = new SemaphoreSlim(maxRequests, maxRequests);
        }
    }

    private HttpClient GetOrCreateHttpClient(Proxy proxy)
    {
        string proxyKey = $"{proxy.ProxyIp}:{proxy.Port}";

        return _proxyStates.GetOrAdd(proxyKey, _ =>
        {
            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxy.ProxyIp, proxy.Port)
                {
                    Credentials = new NetworkCredential(proxy.Username, proxy.Password)
                },
                UseProxy = true,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            return new ProxyState(new HttpClient(handler), _maxRequestsPerProxy);
        }).Client;
    }

    private ProxyState GetOrCreateProxyState(Proxy proxy)
    {
        string proxyKey = $"{proxy.ProxyIp}:{proxy.Port}";

        return _proxyStates.GetOrAdd(proxyKey, _ =>
        {
            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxy.ProxyIp, proxy.Port)
                {
                    Credentials = new NetworkCredential(proxy.Username, proxy.Password)
                },
                UseProxy = true,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            return new ProxyState(new HttpClient(handler), _maxRequestsPerProxy);
        });
    }

    public async Task<T> PostDataObjectAsync<T>(string endpoint, object payload, Dictionary<string, string>? headers = null, Proxy? proxy = null)
    {
        ProxyState? proxyState = null;

        if (proxy != null)
        {
            proxyState = GetOrCreateProxyState(proxy);
            await proxyState.Semaphore.WaitAsync();
        }
        HttpResponseMessage? response = null;
        try
        {
            HttpClient client = proxyState?.Client ?? _httpClient;
            ApplyHeaders(client, headers);

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            response = await client.PostAsync(endpoint, content); // Assign response
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            if (typeof(T) == typeof(string))
            {
                return (T)(object)jsonResponse; // Directly return the string response
            }
            if (!typeof(T).IsPrimitive && !typeof(T).IsValueType && typeof(T) != typeof(string))
            {
                return JsonConvert.DeserializeObject<T>(jsonResponse)!;
            }
            
            return (T)Convert.ChangeType(jsonResponse, typeof(T))!;
        }
        catch (HttpRequestException e)
        {
            string errorDetails = "";

            // Check if the response is available and has a status code
            if (response != null)
            {
                errorDetails += $"Status Code: {response.StatusCode}\n";

                // Read response content if it's a Bad Request (400)
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    errorDetails += $"Response Content: {responseContent}\n";
                }
            }

            // Include exception details
            throw new Exception($"HTTP Error: {errorDetails} Exception: {e.Message}");
        }
        finally
        {
            if (proxyState != null)
            {
                proxyState.Semaphore.Release();
            }
        }
    }

    public async Task<string> PostDataAsync<T>(EmailAccount acc,string endpoint, string payload, Dictionary<string, string>? headers = null, Proxy? proxy = null)
    {
        ProxyState? proxyState = null;

        if (proxy != null)
        {
            proxyState = GetOrCreateProxyState(proxy);
            await proxyState.Semaphore.WaitAsync();
        }

        try
        {
            HttpClient client = proxyState?.Client ?? _httpClient;
            ApplyHeaders(client, headers);

            var content = new MultipartFormDataContent();
            content.Add(new StringContent(payload), "batchJson");

            HttpResponseMessage response = await client.PostAsync(endpoint, content);
           
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException e)
        {
            // Allow HttpRequestException to propagate for Polly to catch
            throw;
        }
        catch (SocketException e)
        {
            // Allow SocketException to propagate for Polly to catch
            throw;
        }
        catch (Exception e)
        {
            if (e.Message.Contains("Proxy error"))
            {
                // Allow specific proxy-related errors to propagate for Polly to catch
                throw;
            }

            // Handle other exceptions or rethrow if necessary
            throw new Exception($"Unexpected error: {e.Message}", e);
        }
        finally
        {
            if (proxyState != null)
            {
                proxyState.Semaphore.Release();
            }
        }
    }

    public async Task<T> GetDataAsync<T>(string endpoint, Dictionary<string, string>? headers = null, Proxy? proxy = null, bool ignoreCache = false)
    {
        ProxyState? proxyState = null;

        if (proxy != null)
        {
            proxyState = GetOrCreateProxyState(proxy);
            await proxyState.Semaphore.WaitAsync();
        }

        try
        {
            HttpClient client = proxyState?.Client ?? _httpClient;

            /*if (!ignoreCache)
            {
                var cachedData = _cacheService.Get<T>(endpoint);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }*/

            if (headers != null) ApplyHeaders(client, headers);
            HttpResponseMessage response = await client.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<T>(jsonResponse)!;

            _cacheService.Set(endpoint, data, TimeSpan.FromMinutes(5));
            return data;
        }
        catch (Exception e)
        {
            throw new Exception($"An error occurred while getting the data: {e.Message}");
        }
        finally
        {
            if (proxyState != null)
            {
                proxyState.Semaphore.Release();
            }
        }
    }
    public async Task DownloadFileAsync(string fileUrl, string savePath, Dictionary<string, string>? headers = null)
    {
        HttpClient client = _httpClient;

        if (headers != null) ApplyHeaders(client, headers);

        using (HttpResponseMessage response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();

            using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                   fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await contentStream.CopyToAsync(fileStream);
            }
        }
    }

    public async Task<bool> PutDataAsync(string endpoint, object payload, Dictionary<string, string>? headers = null, Proxy? proxy = null)
    {
        HttpClient client = proxy != null ? GetOrCreateHttpClient(proxy) : _httpClient;
        ApplyHeaders(client, headers);

        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PutAsync(endpoint, content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteDataAsync(string endpoint, Dictionary<string, string>? headers = null, Proxy? proxy = null)
    {
        HttpClient client = proxy != null ? GetOrCreateHttpClient(proxy) : _httpClient;
        ApplyHeaders(client, headers);

        HttpResponseMessage response = await client.DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }

    private void ApplyHeaders(HttpClient client, Dictionary<string, string>? headers)
    {
        client.DefaultRequestHeaders.Clear();
        if (headers != null)
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }
}
