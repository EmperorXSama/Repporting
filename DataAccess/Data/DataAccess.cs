using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Data;

public class DataAccess : IDataAccess
{
    private readonly IConfiguration _configuration;
    public DataAccess(IConfiguration config)
    {
        _configuration = config;
    }


    public async Task<IEnumerable<T>> LoadDataAsync<T, TU>(string storeProcedure, TU param,
        string connectionStringName = "Default")
    {
        string? connectionString = _configuration.GetConnectionString(connectionStringName);
        
        using IDbConnection connection= new SqlConnection(connectionString);
        
        var rows = await connection.QueryAsync<T>(storeProcedure, param,commandType: CommandType.StoredProcedure);
        
        return rows;
    }

    public async Task SaveDataAsync<T>(string storeProcedure,T param, string? connectionStringName = "Default")
    {
        string? connectionString = _configuration.GetConnectionString(connectionStringName);
        
        using IDbConnection connection= new SqlConnection(connectionString);
        
        await connection.ExecuteAsync(storeProcedure, param, commandType: CommandType.StoredProcedure);
    }
}


























