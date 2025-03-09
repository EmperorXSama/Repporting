using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Reporting.lib.Models.DTO;

namespace Reporting.lib.Data.Services.Proxy;


public class ProxyServices : IProxyServices
{
    private readonly IDataConnection _dbConnection;
    public ProxyServices(IDataConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }
    
    public async  Task<IEnumerable<Models.Core.Proxy>> GetAllProxies()
    {
        return await _dbConnection.LoadDataAsync<Models.Core.Proxy, dynamic>("dbo.GetProxies",new {});
    }
    public async Task SaveProxiesBatchAsync(IEnumerable<ProxyDto> proxies)
    {
        var table = new DataTable();
        table.Columns.Add("ProxyIp", typeof(string));
        table.Columns.Add("Port", typeof(int));
        table.Columns.Add("Username", typeof(string));
        table.Columns.Add("Password", typeof(string));
        table.Columns.Add("Availability", typeof(string));

        foreach (var proxy in proxies)
        {
            table.Rows.Add(
                proxy.ProxyIp,
                proxy.Port,
                proxy.Username,
                proxy.Password,
                proxy.Availability
            );
        }

        var parameters = new { Proxies = table.AsTableValuedParameter("ProxyType") };

        await _dbConnection.SaveDataAsync("[dbo].[AddNewProxies]", parameters);
    }
    public async Task UpdateProxiesBatchAsync(IEnumerable<ProxyUpdateRegion> proxies)
    {
        var table = new DataTable();
        table.Columns.Add("ProxyId", typeof(int));
        table.Columns.Add("Region", typeof(string));
        table.Columns.Add("YahooConnectivity", typeof(string));
        table.Columns.Add("Availability", typeof(string));

        foreach (var proxy in proxies)
        {
            table.Rows.Add(
                proxy.ProxyId,
                proxy.Region,
                proxy.YahooConnectivity,
                proxy.Availability ? "Available" : "Unavailable"
            );
        }

        var parameters = new { ProxyUpdates = table.AsTableValuedParameter("ProxyUpdateTableType") };

        await _dbConnection.SaveDataAsync("[dbo].[UpdateProxiesBatch]", parameters);
    }

    public async Task UpdateReplacedProxiesBatchAsync(IEnumerable<ProxyUpdateDto> proxies)
    {
        var table = new DataTable();
        table.Columns.Add("OldProxyIp", typeof(string));
        table.Columns.Add("OldProxyPort", typeof(int));
        table.Columns.Add("NewProxyIp", typeof(string));
        table.Columns.Add("NewProxyPort", typeof(int));
        table.Columns.Add("NewUsername", typeof(string));
        table.Columns.Add("NewPassword", typeof(string));

        foreach (var proxy in proxies)
        {
            table.Rows.Add(
                proxy.OldProxyIp,
                proxy.OldProxyPort,
                proxy.NewProxyIp,
                proxy.NewProxyPort
            );
        }

        var parameters = new { Proxies = table.AsTableValuedParameter("ProxyReplacementTableType") };

        await _dbConnection.SaveDataAsync("[dbo].[UpdateReplacedProxiesBatch]", parameters);
    }

}