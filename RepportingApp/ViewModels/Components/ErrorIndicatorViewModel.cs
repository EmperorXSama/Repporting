using Avalonia.Media;

namespace RepportingApp.ViewModels.Components;

public enum IndicatorType
{
    Error,
    Success
}

public partial class ErrorIndicatorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _message;   
    [ObservableProperty]
    private string _title; 
    [ObservableProperty]
    private bool _isVisible = false;
    [ObservableProperty]
    private double _opacity;
    [ObservableProperty]
    private IndicatorType _indicatorType = IndicatorType.Error;

    // Observable properties for colors
    [ObservableProperty]
    private string _iconColor = "MainAlert";
    [ObservableProperty]
    private string _closeButtonColor = "MainAlert";
    [ObservableProperty]
    private string _boxShadowColor = "#D86F6F";
    [ObservableProperty]
    private string _gradientTopColor = "#eba298";

    private TaskCompletionSource<bool>? _closedTaskCompletionSource;

    public ErrorIndicatorViewModel()
    {
        IsVisible = false;
        UpdateColors(); // Set initial colors
    }

    // Original method for backward compatibility
    public async Task ShowErrorIndecator(string title, string message)
    {
        Title = title;
        Message = message;
        IndicatorType = IndicatorType.Error;
        IsVisible = true;
        Opacity = 1;
    }

    // New method with type parameter
    public async Task ShowIndicator(string title, string message, IndicatorType type)
    {
        Title = title;
        Message = message;
        IndicatorType = type;
        IsVisible = true;
        Opacity = 1;
    }

    // Method that shows indicator and waits for closure
    public async Task ShowIndicatorAndWait(string title, string message, IndicatorType type)
    {
        Title = title;
        Message = message;
        IndicatorType = type;
        IsVisible = true;
        Opacity = 1;
        
        // Create a new TaskCompletionSource that will complete when the indicator is closed
        _closedTaskCompletionSource = new TaskCompletionSource<bool>();
        
        // Wait until the indicator is closed
        await _closedTaskCompletionSource.Task;
    }

    // Update colors when IndicatorType changes
    partial void OnIndicatorTypeChanged(IndicatorType value)
    {
        UpdateColors();
    }

    private void UpdateColors()
    {
        if (IndicatorType == IndicatorType.Success)
        {
            IconColor = "MainGreen";
            CloseButtonColor = "MainGreen";
            BoxShadowColor = "#6FB86F";
            GradientTopColor = "#b0f5b0";
        }
        else
        {
            IconColor = "MainAlert";
            CloseButtonColor = "MainAlert";
            BoxShadowColor = "#D86F6F";
            GradientTopColor = "#eba298";
        }
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
        
        // Signal that the indicator has been closed
        _closedTaskCompletionSource?.SetResult(true);
        _closedTaskCompletionSource = null; // Reset for next use
    }
}