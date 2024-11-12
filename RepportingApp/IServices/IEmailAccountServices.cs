namespace RepportingApp.IServices;

public interface IEmailAccountServices
{
     Task<Func<CancellationToken, Task>> SendEmailAsync(StartProcessNotifierModel startProcessNotifierModel);
}