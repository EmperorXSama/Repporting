using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace RepportingApp.Views.Components.TaskCardDesign;

public partial class NormalTasks : UserControl
{
    public NormalTasks()
    {
        InitializeComponent();
    }
    
    private async  void OnTaskIdBorderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is TaskInfoUiModel taskInfo)
        {
            var topLevel = TopLevel.GetTopLevel(border);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(taskInfo.TaskId.ToString());

                // Flash background color
                border.Background = Brushes.LightGreen;
                await Task.Delay(500); // Keep color for 0.5 seconds
                border.Background = Brushes.LightGray; // Revert to original color
            }
        }
    }
}