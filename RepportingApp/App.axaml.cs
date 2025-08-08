using RepportingApp.Services;
using Avalonia.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RepportingApp.CoreSystem.ProxyService;
using RepportingApp.Request_Connection_Core.MailBox;
using RepportingApp.CoreSystem.ApiSystem;
using ReportingPageViewModel = RepportingApp.ViewModels.ReportingPageViewModel;

namespace RepportingApp;

public partial class App : Application
{
  
    public static IServiceProvider _ServiceProvider;
    public static IConfiguration? Configuration;
    private IHost _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        LoadConfiguration();
        SetupHostAndServices();
    }

    private void SetupHostAndServices()
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Add your existing services
                services.AddSingleton<IMessenger, StrongReferenceMessenger>();
                services.AddSingleton<IEmailAccountServices, EmailAccountServices>();
                services.AddSingleton<DashboardPageViewModel>();
                services.AddSingleton<AutomationPageViewModel>();
                services.AddSingleton<ProxyManagementPageViewModel>();
                services.AddSingleton<EmailManagementPageViewModel>();
                services.AddSingleton<HomePageViewModel>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<ProcessTrackerPageViewModel>();
                services.AddSingleton<ReportingPageViewModel>();
                services.AddSingleton<MailBoxPageViewModel>();
                services.AddSingleton<TaskInfoManager>();
                services.AddSingleton<IApiConnector, UnifiedApiClient>();
                services.AddSingleton<ICacheService, CacheService>();
                services.AddSingleton<IReportingRequests, ReportingRequests>();
                services.AddSingleton<IProxyApiService, ProxyApiService>();
                services.AddSingleton<IMailBoxRequests, MailBoxRequests>();
                services.AddSingleton<SystemConfigurationEstimator>();
                
            });

        _host = hostBuilder.Build();
        _ServiceProvider = _host.Services;
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Start the host
            await _host.StartAsync();
            
            // Resolve MainWindowViewModel from the DI container
            var mainWindowViewModel = _ServiceProvider.GetService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
            
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static IServiceProvider Services => _ServiceProvider;

    private void LoadConfiguration()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }
    
}