using Avalonia.Media;

namespace RepportingApp.ViewModels.Components;

public partial class ErrorIndicatorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _message;   
    [ObservableProperty]
    private string _title; 
    [ObservableProperty]
    private bool _isVisible = false;
    [ObservableProperty]
    private double opacity;

    public ErrorIndicatorViewModel()
    {
        IsVisible = false;
    }
    public async Task ShowErrorIndecator(string title , string message)
    {
        Title = title;
        Message = message;
        IsVisible = true;
        Opacity = 1;
       
    }
    
    [RelayCommand]
    public async Task CloseErrorIndecator()
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