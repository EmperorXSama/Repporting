using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace RepportingApp.CoreSystem.ApiSystem;

public class UnifiedApiClient : IApiConnector
{
    
    private readonly HttpClient _httpClient;
    private readonly ICacheService _cacheService;

    public UnifiedApiClient(ICacheService cacheService)
    {
        _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });
        _cacheService = cacheService;
    }
    private HttpClient CreateHttpClientWithProxy(Proxy? proxy)
    {
        var httpClientHandler = new HttpClientHandler
        {
            Proxy = new WebProxy(proxy.ProxyIp, proxy.Port)
            {
                Credentials = new NetworkCredential(proxy.Username, proxy.Password)
            },
            UseProxy = true,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        return new HttpClient(httpClientHandler);
    }
    public async Task<T> PostDataObjectAsync<T>(string endpoint, object payload, Dictionary<string, string>? headers = null,Proxy? proxy = null)
    {
        try
        {
            HttpClient client = proxy != null ? CreateHttpClientWithProxy(proxy) : _httpClient;
            ApplyHeaders(client,headers);
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(jsonResponse)!;
        }
        catch (Exception e)
        {
            throw new Exception($"An error occured while posting the data: {e.Message}");
        }
      
    }
    public async Task<string> PostDataAsync<T>(string endpoint, string payload ,Dictionary<string, string>? headers = null,Proxy? proxy = null)
    {
        try
        {
            HttpClient client = proxy != null ? CreateHttpClientWithProxy(proxy) : _httpClient;
            ApplyHeaders(client,headers);
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(payload), "batchJson");
            HttpResponseMessage response = await client.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse;
        }
        catch (Exception e)
        {
            throw new Exception($"An error occured while posting the data: {e.Message}");
        }
      
    }
    public async Task<T> GetDataAsync<T>(string endpoint, Dictionary<string, string>? headers = null,Proxy? proxy = null,bool ignoreCache = false)
    {
        // Attempt to retrieve from cache first
        HttpClient client = proxy != null ? CreateHttpClientWithProxy(proxy) : _httpClient;
        var cachedData = _cacheService.Get<T>(endpoint);
        if (cachedData != null && !ignoreCache)
        {
            return cachedData;
        }

        ApplyHeaders(client,headers);
        HttpResponseMessage response = await client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<T>(jsonResponse)!;

        // Store the data in cache with a custom expiration time (e.g., 5 minutes)
        _cacheService.Set(endpoint, data, TimeSpan.FromMinutes(5));

        return data;
    }
    
    


    public async Task<bool> PutDataAsync(string endpoint, object payload, Dictionary<string, string>? headers = null,Proxy? proxy = null)
    {
        HttpClient client = proxy != null ? CreateHttpClientWithProxy(proxy) : _httpClient;
        ApplyHeaders(client,headers);
        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PutAsync(endpoint, content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteDataAsync(string endpoint, Dictionary<string, string>? headers = null,Proxy? proxy = null)
    {
        HttpClient client = proxy != null ? CreateHttpClientWithProxy(proxy) : _httpClient;
        ApplyHeaders(client,headers);
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