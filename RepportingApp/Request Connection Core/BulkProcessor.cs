namespace RepportingApp.Request_Connection_Core;

public static class BulkProcessor<T>
{
    public static async Task ProcessItemsAsync(
        IEnumerable<T> items,
        Func<IEnumerable<T>,Task> bulkProcessor,
        Func<T,Task> singleProcessor,
        int bulkThreshold,
        int bulkChunkSize,
        int singleThreshold)
    {
        var itemsList = items.ToList();
        int totalItems = itemsList.Count;


        if (totalItems > bulkThreshold)
        {
            while (itemsList.Count > singleThreshold)
            {
                var chunk = itemsList.Take(bulkChunkSize).ToList();
                await bulkProcessor(chunk);
                itemsList = itemsList.Skip(bulkChunkSize).ToList();
            }
        }

        foreach (var item in itemsList)
        {
            await singleProcessor(item);
        }
    }
}