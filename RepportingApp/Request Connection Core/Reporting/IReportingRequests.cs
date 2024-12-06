namespace RepportingApp.Request_Connection_Core.Reporting;

public interface IReportingRequests
{
    Task<bool> SendReportAsync(EmailAccount emailAccount,string messageId);

    Task<ReturnTypeObject> ProcessMarkMessagesAsNotSpam(EmailAccount emailAccount,
        MarkMessagesAsReadConfig config);
    Task<ReturnTypeObject> ProcessGetMessagesFromDir(EmailAccount emailAccount, string directoryId);

    Task<ReturnTypeObject> ProcessMarkMessagesAsReadFromDir(EmailAccount emailAccount, MarkMessagesAsReadConfig config,string directoryId = "1");
}