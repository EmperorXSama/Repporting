using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using RepportingApp.ViewModels;
using RepportingApp.Views;

namespace RepportingApp;

public partial class App : Application
{

    public static IServiceProvider _ServiceProvider;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        this.AttachDevTools(); // This line enables the developer tools
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
}