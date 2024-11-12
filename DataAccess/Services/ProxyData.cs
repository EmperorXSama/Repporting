using DataAccess.Data;

namespace DataAccess.Services;

public class ProxyData
{
    private readonly IDataAccess _dataAccess;

    public ProxyData(IDataAccess dataAccess)
    {
        this._dataAccess = dataAccess;
    }
    
}