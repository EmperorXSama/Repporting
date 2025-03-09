using Reporting.lib.Models.DTO;

namespace Reporting.lib.Data.Services.Proxy;

public interface IProxyServices
{
    Task<IEnumerable<Models.Core.Proxy>> GetAllProxies();
    Task SaveProxiesBatchAsync(IEnumerable<ProxyDto> proxies);
    Task UpdateReplacedProxiesBatchAsync(IEnumerable<ProxyUpdateDto> proxies);
    Task UpdateProxiesBatchAsync(IEnumerable<ProxyUpdateRegion> proxies);
}