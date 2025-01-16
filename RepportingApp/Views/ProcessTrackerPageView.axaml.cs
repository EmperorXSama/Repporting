using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace RepportingApp.Views;

public partial class ProcessTrackerPageView : UserControl
{
    public ProcessTrackerPageView()
    {
        InitializeComponent();
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is TaskInfoUiModel selectedTask)
        {
           
            var viewModel = DataContext as ProcessTrackerPageViewModel;
            if (viewModel != null)
            {
                var tasks = viewModel.NotificationTasks;
                viewModel.SelectedTask = selectedTask;
                foreach (var task in tasks)
                {
                    task.IsSelected = false;
                }

                // Select the current task
                selectedTask.IsSelected = true;
            }
        }
    }
}