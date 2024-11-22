namespace RepportingApp.Request_Connection_Core.Reporting;

public interface IReportingRequests
{
    Task<bool> SendReportAsync(EmailAccount emailAccount,string messageId);
    Task ProcessGetMessagesFromInbox(EmailAccount emailAccount, int thread);

    Task<int> ProcessMarkMessagesAsReadFromInbox(EmailAccount emailAccount, int bulkThreshold = 60,
        int bulkChunkSize = 30, int singleThreshold = 20, IEnumerable<InboxMessages>? messages = null);
}