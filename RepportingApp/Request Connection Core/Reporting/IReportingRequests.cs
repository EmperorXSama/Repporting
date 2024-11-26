namespace RepportingApp.Request_Connection_Core.Reporting;

public interface IReportingRequests
{
    Task<bool> SendReportAsync(EmailAccount emailAccount,string messageId);
    Task<ReturnTypeObject> ProcessGetMessagesFromDir(EmailAccount emailAccount, string directoryId);

    Task<ReturnTypeObject> ProcessMarkMessagesAsReadFromDir(EmailAccount emailAccount, int bulkThreshold = 60,
        int bulkChunkSize = 30, int singleThreshold = 20,string directoryId =Statics.InboxDir , IEnumerable<InboxMessages>? messages = null);
}