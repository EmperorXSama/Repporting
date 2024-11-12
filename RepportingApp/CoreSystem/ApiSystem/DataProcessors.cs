namespace RepportingApp.CoreSystem.ApiSystem;

public class DataProcessors
{
    public IEnumerable<T> FilterData<T>(IEnumerable<T> data, Func<T, bool> predicate)
    {
        return data.Where(predicate);
    }

    public void TransformData<T>(IEnumerable<T> data, Action<T> transformation)
    {
        foreach (var item in data)
        {
            transformation(item);
        }
    }
}