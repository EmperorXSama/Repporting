using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace RepportingApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly Dictionary<Type, ViewModelBase> _viewModelCache = new();
    
    [ObservableProperty] private bool _isPaneOpen = false;
    [ObservableProperty] private ViewModelBase _currentPage = GetOrCreateViewModel(typeof(HomePageViewModel));
    [ObservableProperty] private ListItemTemplate _selectedListItem;

    public MainWindowViewModel(IMessenger messenger) : base(messenger)
    {
      
    }

    partial void OnSelectedListItemChanged(ListItemTemplate? value)
    {
        if (value is null) return;
        CurrentPage = GetOrCreateViewModel(value.ModelType);
    }
    public ObservableCollection<ListItemTemplate> navigationItems { get; } = new()
    {
        new ListItemTemplate(typeof(HomePageViewModel),"HomeIcon"),
        new ListItemTemplate(typeof(DashboardPageViewModel),"Dashboard"),
        new ListItemTemplate(typeof(AutomationPageViewModel),"AutomationIcon"),
        new ListItemTemplate(typeof(ProxyManagementPageViewModel),"ProxyIcon"),
        new ListItemTemplate(typeof(ReportingPageViewModel),"Reporting"),
    };


    private static ViewModelBase GetOrCreateViewModel(Type viewBaseModelType)
    {
        if (_viewModelCache.TryGetValue(viewBaseModelType, out var viewModel))
        {
            return viewModel;
        }
        
        var instance = (ViewModelBase)App.Services.GetRequiredService(viewBaseModelType);
        _viewModelCache[viewBaseModelType] = instance;
        return instance;
    }
    
    [RelayCommand]
    private void OpenClosePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    [RelayCommand]
    private void MinimizeWindow()
    {
        var window = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window != null) window.WindowState = WindowState.Minimized;
    }
    [RelayCommand]
    private void MaximizeWindow()
    {
        var window = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
    [RelayCommand]
    private void CloseWindow()
    {
        var window = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        window?.Close();
    }
}
public class ListItemTemplate
{
    public ListItemTemplate(Type type, string iconkey)
    {
        ModelType = type;
        Label = type.Name.Replace("PageViewModel", "");

        Application.Current!.TryFindResource(iconkey, out var res);
        icon = (StreamGeometry)res!;
    }
    
    public string Label { get; }
    public Type ModelType { get;}
    public StreamGeometry icon { get; set; }
}