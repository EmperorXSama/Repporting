using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RepportingApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Hello";
    [ObservableProperty] private bool _isPaneOpen = false;
    [ObservableProperty] private ViewModelBase _currentPage = new HomePageViewModel();
    [ObservableProperty] private ListItemTemplate _selectedListItem;

    partial void OnSelectedListItemChanged(ListItemTemplate? value)
    {
        if (value is null) return;

        var instance = Activator.CreateInstance(value.ModelType);
        if (instance is null) return;
        CurrentPage = (ViewModelBase)instance;
    }
    public ObservableCollection<ListItemTemplate> navigationItems { get; } = new()
    {
        new ListItemTemplate(typeof(HomePageViewModel),"HomeIcon"),
        new ListItemTemplate(typeof(DashboardPageViewModel),"Dashboard"),
        new ListItemTemplate(typeof(AutomationPageViewModel),"AutomationIcon"),
        new ListItemTemplate(typeof(ProxyManagementPageViewModel),"ProxyIcon"),
    };

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