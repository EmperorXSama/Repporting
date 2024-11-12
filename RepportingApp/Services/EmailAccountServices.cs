using System.Diagnostics;
using RepportingApp.IServices;

namespace RepportingApp.Services;

public class EmailAccountServices : IEmailAccountServices
{
    public async Task<Func<CancellationToken, Task>> SendEmailAsync(StartProcessNotifierModel startProcessNotifierModel)
    {
        
        Func<CancellationToken, Task> taskFunc = async token =>
        {
            Debug.WriteLine("working on list of emails ...");
            Debug.WriteLine(startProcessNotifierModel.ProcessDate);
            Debug.WriteLine(startProcessNotifierModel.Thread);
            Debug.WriteLine(startProcessNotifierModel.SelectedProxySetting.ToString());
            Debug.WriteLine(startProcessNotifierModel.SelectedReportSetting.ToString());
            Debug.WriteLine(startProcessNotifierModel.Interval.ToString());
           await Task.Delay(5000, token);
          
        };
        return taskFunc;
    }
}