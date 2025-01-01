namespace Reporting.lib.ExtensionMethods;

public static class EnumerableExtensions
{
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int batchSize)
    {
        var batch = new List<T>(batchSize);
        foreach (var item in items)
        {
            batch.Add(item);
            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }
        if (batch.Count > 0)
        {
            yield return batch;
        }
    }
    public static  List<List<T>> SplitList<T>(this List<T> source, int chunks)
    {
        var result = new List<List<T>>();
        int chunkSize = (int)Math.Ceiling(source.Count / (double)chunks);

        for (int i = 0; i < source.Count; i += chunkSize)
        {
            result.Add(source.GetRange(i, Math.Min(chunkSize, source.Count - i)));
        }
        return result;
    }

}