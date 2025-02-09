namespace RepportingApp.Request_Connection_Core.Reporting;

public interface IReportingRequests
{

    Task< List<ReturnTypeObject>> ProcessMarkMessagesAsNotSpam(EmailAccount emailAccount,
        MarkMessagesAsReadConfig config);
    Task<ReturnTypeObject> ProcessGetMessagesFromDir(EmailAccount emailAccount, string directoryId);

    Task<List<ReturnTypeObject>> ProcessMarkMessagesAsReadFromDirs(EmailAccount emailAccount,
        MarkMessagesAsReadConfig config, List<string> directoryIds);

    Task< List<ReturnTypeObject>> ProcessArchiveMessages(EmailAccount emailAccount,
        MarkMessagesAsReadConfig config, string directoryId = "1");

    Task<List<ReturnTypeObject>>
        ProcessGetMessagesFromDirs(EmailAccount emailAccount, IEnumerable<string> directoryIds);
}