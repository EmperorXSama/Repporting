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
    private bool _isVisible = false;
    [ObservableProperty]
    private string iconColor = "#519872";  
    [ObservableProperty]
    private StreamGeometry icon;

    [ObservableProperty]
    private double opacity;

    public async Task ShowToast(string message,string iconName)
    {
        Message = message;
       

        Application.Current!.TryFindResource(iconName, out var res);
        Icon = (StreamGeometry)res!;
        
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
    }
}