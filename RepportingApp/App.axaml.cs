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
    private IConfigurationMonitorService _configMonitor;

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

                // Add the configuration monitor service
                services.AddSingleton<IConfigurationMonitorService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<AzureConfigurationMonitorService>>();
                    var connectionString = "Endpoint=https://repporting.azconfig.io;Id=O1c2;Secret=8zQzxPiOz0SIStac2WRI8jRokcQSpaMpT6w0RGoXNJdAevuTbwRwJQQJ99BEAC5RqLJpihN2AAACAZACPiW7";
                    
                    // Use "NewSystemBeta" as the key name and check every 8 hours
                    return new AzureConfigurationMonitorService(
                        logger, 
                        connectionString, 
                        "NewSystemBeta", 
                        TimeSpan.FromHours(8)
                    );
                });
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

            // Get the configuration monitor service and start monitoring
            _configMonitor = _host.Services.GetRequiredService<IConfigurationMonitorService>();
            
            var startDate = new DateTime(2025, 8, 1);
            if (DateTime.Today >= startDate)
            {
                await _configMonitor.StartMonitoringAsync();
                Console.WriteLine("Configuration monitoring started.");
            }
            else
            {
                Console.WriteLine($"Configuration monitoring will start on {startDate:yyyy-MM-dd}. Current date: {DateTime.Today:yyyy-MM-dd}");
            }

            // Resolve MainWindowViewModel from the DI container
            var mainWindowViewModel = _ServiceProvider.GetService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            // Handle application shutdown
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
            desktop.Exit += OnApplicationExit;
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

    private async void OnApplicationExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            Console.WriteLine("Application is shutting down...");
            _configMonitor?.StopMonitoring();
            
            if (_host != null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during application shutdown: {ex.Message}");
        }
    }
}