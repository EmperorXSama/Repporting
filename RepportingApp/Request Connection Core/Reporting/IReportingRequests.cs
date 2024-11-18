namespace RepportingApp.Request_Connection_Core.Reporting;

public interface IReportingRequests
{
    Task<bool> SendReportAsync(EmailAccount emailAccount,string messageId);
    Task<ObservableCollection<InboxMessages>> GetMessagesFromInboxFolder(EmailAccount emailAccount );
}