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
    public async Task<T> GetDataAsync<T>(string endpoint, Dictionary<string, string>? headers = null)
    {
        // Attempt to retrieve from cache first
        var cachedData = _cacheService.Get<T>(endpoint);
        if (cachedData != null)
        {
            return cachedData;
        }

        ApplyHeaders(headers);
        HttpResponseMessage response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<T>(jsonResponse)!;

        // Store the data in cache with a custom expiration time (e.g., 5 minutes)
        _cacheService.Set(endpoint, data, TimeSpan.FromMinutes(5));

        return data;
    }
    
    
    public async Task<T> PostDataAsync<T>(string endpoint, object payload, Dictionary<string, string>? headers = null)
    {
        try
        {
            ApplyHeaders(headers);
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(jsonResponse)!;
        }
        catch (Exception e)
        {
            throw new Exception($"An error occured while posting the data: {e.Message}");
        }
      
    }

    public async Task<bool> PutDataAsync(string endpoint, object payload, Dictionary<string, string>? headers = null)
    {
        ApplyHeaders(headers);
        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PutAsync(endpoint, content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteDataAsync(string endpoint, Dictionary<string, string>? headers = null)
    {
        ApplyHeaders(headers);
        HttpResponseMessage response = await _httpClient.DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }
    private void ApplyHeaders(Dictionary<string, string>? headers)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        if (headers != null)
        {
            foreach (var header in headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }
}