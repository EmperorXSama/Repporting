using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RepportingApp.ViewModels;

public partial class ToastNotificationViewModel :ObservableObject
{
    [ObservableProperty]
    private string _message;   
    [ObservableProperty]
    private string _title; 
    [ObservableProperty]
    private bool _isVisible = false;
    [ObservableProperty]
    private string iconColor = "#519872";  
    [ObservableProperty]
    private StreamGeometry icon;

    [ObservableProperty]
    private double opacity;

    
    public Action<ToastNotificationViewModel> RemoveToast { get; set; }
    public ToastNotificationViewModel(Action<ToastNotificationViewModel> removeToastCallback)
    {
        RemoveToast = removeToastCallback;
    }

    public async Task ShowToast(string title , string message,string iconName)
    {
        Title = title;
        Message = message;

        Application.Current!.TryFindResource(iconName, out var res);
        Icon = (StreamGeometry)res!;

        if (iconName == "Warning") 
        {
            IconColor = "#F1BF98";
        }else if (iconName == "Success")
        {
            IconColor = "#519872";
        }else if (iconName == "Error")
        {
            IconColor = "#FF6B6B";
        }
        IsVisible = true;
        Opacity = 1;
       
    }
    
    [RelayCommand]
    public async Task CloseToast()
    {
        for (double i = 1; i >= 0; i -= 0.05)
        {
            Opacity = i;
            await Task.Delay(20);
        }
        Opacity = 0;
        IsVisible = false;
        RemoveToast?.Invoke(this);
    }

}