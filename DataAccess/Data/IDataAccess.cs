namespace DataAccess.Data;

public interface IDataAccess
{
    Task<IEnumerable<T>> LoadDataAsync<T, TU>(string storeProcedure, TU param,
        string connectionStringName = "Default");

    Task SaveDataAsync<T>(string storeProcedure,T param, string? connectionStringName = "Default");
}