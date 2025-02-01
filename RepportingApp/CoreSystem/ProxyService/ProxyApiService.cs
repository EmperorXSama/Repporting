namespace RepportingApp.CoreSystem.ProxyService;

public class ProxyApiService : IProxyApiService
{

    private readonly IApiConnector _apiConnector;

    public ProxyApiService(IApiConnector apiConnector)
    {
        _apiConnector = apiConnector;
    }
    public async Task<List<ProxyApiModelResults>> GetAllReplacedProxiesAsync()
    {
        var headers = PopulateHeaders();
        List<ProxyApiModelResults> allProxies = new List<ProxyApiModelResults>();
        string nextUrl = ApiEndPoints.GetReplacedProxies; 

        while (!string.IsNullOrEmpty(nextUrl))
        {
            var result = await _apiConnector.GetDataAsync<PeroxyApiRootObject>(nextUrl, headers, ignoreCache: true);

            if (result?.results != null)
                allProxies.AddRange(result.results);

            nextUrl = result?.next; 
        }

        return allProxies;
    }
    public async Task DownloadProxyListAsync()
    {
        var headers = PopulateHeaders();
        string fileUrl = "https://proxy.webshare.io/api/v2/proxy/list/download/suwxahurffmqwjqmjspgprielmhabzmxyueofqbr/-/any/username/direct/-/";
        string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "proxies.txt");

        await _apiConnector.DownloadFileAsync(fileUrl, savePath, headers);
        Console.WriteLine($"Proxy list downloaded successfully to: {savePath}");
    }

    private Dictionary<string, string> PopulateHeaders()
    {
        return new Dictionary<string, string>
        {
            { "Authorization", "Token 8khbeisbwt8jwn1729tgl4c2zqb9qtra7r0dz9bp" },
        };
    }
}

public interface IProxyApiService
{
    Task<List<ProxyApiModelResults>> GetAllReplacedProxiesAsync();
    Task DownloadProxyListAsync();
}