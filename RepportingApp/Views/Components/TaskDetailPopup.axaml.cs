using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace RepportingApp.Views.Components;

public partial class TaskDetailPopup : UserControl
{
    public TaskDetailPopup()
    {
        InitializeComponent();
    }
    private async Task CopyEmailToClipboard(string emailAddress)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null && !string.IsNullOrEmpty(emailAddress))
        {
            await clipboard.SetTextAsync(emailAddress);
                
            // Optional: Show feedback through the ViewModel
            if (DataContext is TaskInfoUiModel viewModel)
            {
                viewModel.DownloadValue = $"Copied: {emailAddress}";
                    
                // Clear message after 2 seconds
                await Task.Delay(2000);
                viewModel.DownloadValue = "";
            }
        }
    }

    // Handle pointer pressed on list items
    private async void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ItemInfo itemInfo)
        {
            if (itemInfo.Email?.EmailAddress != null)
            {
                await CopyEmailToClipboard(itemInfo.Email.EmailAddress);
                e.Handled = true;
            }
        }
    }
}