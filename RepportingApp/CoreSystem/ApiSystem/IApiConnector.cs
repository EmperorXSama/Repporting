namespace RepportingApp.CoreSystem.ApiSystem;

public interface IApiConnector
{
    Task<T> GetDataAsync<T>(string endpoint, Dictionary<string, string>? headers = null,Proxy? proxy = null,bool ignoreCache = false);

    Task<string> PostDataAsync<T>(string endpoint, string payload,
        Dictionary<string, string>? headers = null, Proxy? proxy = null);
    Task<T> PostDataObjectAsync<T>(string endpoint, object payload, Dictionary<string, string>? headers = null,Proxy? proxy = null);
    Task<bool> PutDataAsync(string endpoint, object payload, Dictionary<string, string>? headers = null,Proxy? proxy = null);
    Task<bool> DeleteDataAsync(string endpoint, Dictionary<string, string>? headers = null,Proxy? proxy = null);
}