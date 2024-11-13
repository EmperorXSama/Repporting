using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Reporting.lib.Data;

public class DataConnection : IDataConnection
{
    private readonly IConfiguration _configuration;

    public DataConnection(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    public async Task<IEnumerable<T>> LoadDataAsync<T, TU>(string storeProcedure, TU param,
        string connectionStringName = "Default")
    {
        string? connectionString = _configuration.GetConnectionString(connectionStringName);
    
        using IDbConnection connection = new SqlConnection(connectionString);
    
        var rows = await connection.QueryAsync<T>(storeProcedure, param, commandType: CommandType.StoredProcedure);
    
        return rows;
    }

    public async Task<IEnumerable<TPrimary>> LoadDataWithMappingAsync<TPrimary, TSecondary, TU>(
        string storeProcedure, 
        TU param, 
        Func<TPrimary, TSecondary, TPrimary> map, 
        string splitOn, 
        string connectionStringName = "Default")
    {
        string? connectionString = _configuration.GetConnectionString(connectionStringName);

        using IDbConnection connection = new SqlConnection(connectionString);

        var result = await connection.QueryAsync<TPrimary, TSecondary, TPrimary>(
            storeProcedure, 
            map,
            param, 
            commandType: CommandType.StoredProcedure,
            splitOn: splitOn
        );

        return result;
    }
    public async Task<IEnumerable<TPrimary>> LoadDataWithMappingAsync<TPrimary, TSecondary, TThird, TU>(
        string storeProcedure, 
        TU param, 
        Func<TPrimary, TSecondary, TThird, TPrimary> map, 
        string splitOn, 
        string connectionStringName = "Default")
    {
        string? connectionString = _configuration.GetConnectionString(connectionStringName);

        using IDbConnection connection = new SqlConnection(connectionString);

        var result = await connection.QueryAsync<TPrimary, TSecondary, TThird, TPrimary>(
            storeProcedure, 
            map,
            param, 
            commandType: CommandType.StoredProcedure,
            splitOn: splitOn
        );

        return result;
    }


    public async Task<int> SaveDataAsync<T>(string storeProcedure, T param, string connectionStringName = "Default")
    {
        string? connectionString = _configuration.GetConnectionString(connectionStringName);
    
        using IDbConnection connection = new SqlConnection(connectionString);
    
        // Use QuerySingleOrDefaultAsync and return 0 if no result is found
        var newId = await connection.QuerySingleOrDefaultAsync<int>(
            storeProcedure, 
            param, 
            commandType: CommandType.StoredProcedure
        );

        // If no result is found, return 0 or some default value
        return newId == 0 ? 0 : newId;
    }

}