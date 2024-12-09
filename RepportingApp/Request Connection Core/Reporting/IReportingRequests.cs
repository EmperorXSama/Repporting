namespace RepportingApp.Request_Connection_Core.Reporting;

public interface IReportingRequests
{

    Task<ReturnTypeObject> ProcessMarkMessagesAsNotSpam(EmailAccount emailAccount,
        MarkMessagesAsReadConfig config);
    Task<ReturnTypeObject> ProcessGetMessagesFromDir(EmailAccount emailAccount, string directoryId);

    Task<ReturnTypeObject> ProcessMarkMessagesAsReadFromDir(EmailAccount emailAccount, MarkMessagesAsReadConfig config,string directoryId = "1");

    Task<ReturnTypeObject> ProcessArchiveMessages(EmailAccount emailAccount,
        MarkMessagesAsReadConfig config, string directoryId = "1");
}