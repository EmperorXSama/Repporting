using System;
using Microsoft.Extensions.DependencyInjection;
using RepportingApp.ViewModels;

namespace RepportingApp;

public class ViewModelLocator
{
    private readonly IServiceProvider _serviceProvider;

    public ViewModelLocator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T GetViewModel<T>() where T : ViewModelBase
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    // You can add properties for specific view models if needed
    public DashboardPageViewModel DashboardPageVM => GetViewModel<DashboardPageViewModel>();
    public AutomationPageViewModel AutomationPageVM => GetViewModel<AutomationPageViewModel>();
    public ProxyManagementPageViewModel ProxyManagementPageVM => GetViewModel<ProxyManagementPageViewModel>();
}