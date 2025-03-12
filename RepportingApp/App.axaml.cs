
using RepportingApp.Services;
using Avalonia.Diagnostics;
using RepportingApp.CoreSystem.ProxyService;
using RepportingApp.Request_Connection_Core.MailBox;
using ReportingPageViewModel = RepportingApp.ViewModels.ReportingPageViewModel;

namespace RepportingApp;

public partial class App : Application
{

    public static IServiceProvider _ServiceProvider;
    public static IConfiguration? Configuration;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        LoadConfiguration();
        var configService = ShutdownChecker.Instance;
    }
    

    public override void OnFrameworkInitializationCompleted()
    {

        #region DI

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IMessenger, StrongReferenceMessenger>();
        serviceCollection.AddSingleton<IEmailAccountServices, EmailAccountServices>();
        serviceCollection.AddSingleton<DashboardPageViewModel>();
        serviceCollection.AddSingleton<AutomationPageViewModel>();
        serviceCollection.AddSingleton<ProxyManagementPageViewModel>();
        serviceCollection.AddSingleton<EmailManagementPageViewModel>();
        serviceCollection.AddSingleton<HomePageViewModel>();
        serviceCollection.AddSingleton<MainWindowViewModel>();
        serviceCollection.AddSingleton<ProcessTrackerPageViewModel>();
        serviceCollection.AddSingleton<ReportingPageViewModel>();
        serviceCollection.AddSingleton<MailBoxPageViewModel>();
        serviceCollection.AddSingleton<TaskInfoManager>();


        serviceCollection.AddSingleton<IApiConnector, UnifiedApiClient>();
        serviceCollection.AddSingleton<ICacheService, CacheService>();
        serviceCollection.AddSingleton<IReportingRequests, ReportingRequests>();
        serviceCollection.AddSingleton<IProxyApiService, ProxyApiService>();
        serviceCollection.AddSingleton<IMailBoxRequests, MailBoxRequests>();
        
        
        
        serviceCollection.AddSingleton<SystemConfigurationEstimator>();
        _ServiceProvider = serviceCollection.BuildServiceProvider();
        
        #endregion
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Resolve MainWindowViewModel from the DI container
            var mainWindowViewModel = _ServiceProvider.GetService<MainWindowViewModel>();
        
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel // Inject the resolved ViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
    public static IServiceProvider Services => _ServiceProvider;

    private void LoadConfiguration()
    {
        Configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json",optional:false,reloadOnChange:true).Build();
       
    }
}