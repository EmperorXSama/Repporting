using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace RepportingApp.Views;

public partial class MailBoxPageView : UserControl
{
    public MailBoxPageView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MailBoxPageViewModel>();
    }
    
    private void DataGrid_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.C) // Fix: Using KeyModifiers instead of ModifierKeys
        {
            CopySelectedRows();
            e.Handled = true; // Prevents default copy behavior
        }
        
    }
    
    private async Task CopySelectedRows()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (DataContext is MailBoxPageViewModel viewModel )
        {
            if (DataGrid.SelectedItems.Count > 0)
            {
                var copiedText = DataGrid.SelectedItems
                    .OfType<EmailMailboxDetails>()
                    .Select(e => $"{e.EmailAddress}")
                    .Aggregate((current, next) => $"{current}\n{next}"); // Newline-separated rows
                await clipboard.SetTextAsync(copiedText);
            }
        }

      
    }
}