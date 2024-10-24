using System.Collections.Generic;
using DataAccess.Models;

namespace RepportingApp.GeneralData;

public static class ProxyData
{
    public static List<ProxyModel> GetSampleProxyData()
    {
        return new List<ProxyModel>
        {
            new ProxyModel { Ip = "192.168.1.1", Port = "8080", Region = "Australia" },
            new ProxyModel { Ip = "192.168.1.2", Port = "8081", Region = "Australia" },
            new ProxyModel { Ip = "192.168.2.1", Port = "8080", Region = "Indonesia" },
            new ProxyModel { Ip = "192.168.2.2", Port = "8080", Region = "Indonesia" },
            new ProxyModel { Ip = "192.168.3.1", Port = "8080", Region = "Thailand" },
            new ProxyModel { Ip = "192.168.4.1", Port = "8080", Region = "Germany" }
        };
    }
}