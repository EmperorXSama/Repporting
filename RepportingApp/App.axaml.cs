
using RepportingApp.ViewModels.ExtensionViewModel;
using ReportingPageViewModel = RepportingApp.ViewModels.ReportingPageViewModel;

namespace RepportingApp;

public partial class App : Application
{

    public static IServiceProvider _ServiceProvider;
    public static IConfiguration? Configuration;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        this.AttachDevTools(); // This line enables the developer tools
        LoadConfiguration();
    }

    public override void OnFrameworkInitializationCompleted()
    {

        #region DI

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IMessenger, StrongReferenceMessenger>();
        serviceCollection.AddSingleton<DashboardPageViewModel>();
        serviceCollection.AddSingleton<AutomationPageViewModel>();
        serviceCollection.AddSingleton<ProxyManagementPageViewModel>();
        serviceCollection.AddSingleton<HomePageViewModel>();
        serviceCollection.AddSingleton<MainWindowViewModel>();
        serviceCollection.AddSingleton<ReportingPageViewModel>();
        serviceCollection.AddSingleton<TaskInfoManager>();
        
        
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