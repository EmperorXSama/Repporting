namespace RepportingApp.ViewModels.Components;

public partial class SuccessEndicatoreViewModel : ObservableObject
{
    [ObservableProperty]
    private string _message;   
    [ObservableProperty]
    private string _title; 
    [ObservableProperty]
    private bool _isVisible = false;
    [ObservableProperty]
    private double opacity;
    

    public SuccessEndicatoreViewModel()
    {
        IsVisible = false;
    }

    public async Task ShowSuccessIndecator(string title, string message)
    {
        Title = title;
        Message = message;
        IsVisible = true;
        Opacity = 1;
        
    }
    
    [RelayCommand]
    public async Task CloseSuccessIndecator()
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