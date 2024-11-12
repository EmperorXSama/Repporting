namespace RepportingApp.CoreSystem.ApiSystem;

public interface IApiConnector
{
    Task<T> GetDataAsync<T>(string endpoint, Dictionary<string, string>? headers = null);
    Task<T> PostDataAsync<T>(string endpoint, object payload, Dictionary<string, string>? headers = null);
    Task<bool> PutDataAsync(string endpoint, object payload, Dictionary<string, string>? headers = null);
    Task<bool> DeleteDataAsync(string endpoint, Dictionary<string, string>? headers = null);
}