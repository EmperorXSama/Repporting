namespace Reporting.lib.Data;

public interface IDataConnection
{
    Task<IEnumerable<T>> LoadDataAsync<T, TU>(string storeProcedure, TU param,
        string connectionStringName = "Default");

    Task<int> SaveDataAsync<T>(string storeProcedure, T param, string connectionStringName = "Default");

    Task<IEnumerable<TPrimary>> LoadDataWithMappingAsync<TPrimary, TSecondary, TU>(
        string storeProcedure,
        TU param,
        Func<TPrimary, TSecondary, TPrimary> map,
        string splitOn,
        string connectionStringName = "Default");

    Task<IEnumerable<TPrimary>> LoadDataWithMappingAsync<TPrimary, TSecondary, TThird, TU>(
        string storeProcedure,
        TU param,
        Func<TPrimary, TSecondary, TThird, TPrimary> map,
        string splitOn,
        string connectionStringName = "Default");

    Task<IEnumerable<TPrimary>> LoadDataWithMappingAsync<TPrimary, TSecondary, TThird, TFourth, TU>(
        string storeProcedure,
        TU param,
        Func<TPrimary, TSecondary, TThird, TFourth, TPrimary> map,
        string splitOn,
        string connectionStringName = "Default");
}