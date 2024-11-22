namespace RepportingApp.Models.RequestModels;

public class SingleBulkModel
{
    public int BulkInterval { get; set; }
    public int BulkChunkCount { get; set; }
    public int SingleInterval { get; set; }
}