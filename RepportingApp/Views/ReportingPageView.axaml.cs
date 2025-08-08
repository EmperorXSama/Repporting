using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using ExCSS;
using Microsoft.Extensions.DependencyInjection;
using RepportingApp.ViewModels;
using Color = Avalonia.Media.Color;
using ReportingPageViewModel = RepportingApp.ViewModels.ReportingPageViewModel;

namespace RepportingApp.Views;

public partial class ReportingPageView : UserControl
{
    public ReportingPageView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ReportingPageViewModel>();
        AdjustGridColumns(true);
        this.AttachedToVisualTree += OnAttachedToVisualTree;
        this.DetachedFromVisualTree += OnDetachedFromVisualTree;
    }
     
    private void OnAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window != null)
        {
            window.Deactivated += OnWindowDeactivated;
            window.PositionChanged += OnWindowPositionChanged;
        }
    }
    
    private void OnDetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window != null)
        {
            window.Deactivated -= OnWindowDeactivated;
            window.PositionChanged -= OnWindowPositionChanged;
        }
    }
    
    private void OnWindowDeactivated(object sender, EventArgs e)
    {
        // Close popup when window loses focus
        if (DataContext is ReportingPageViewModel viewModel)
        {
            viewModel.IsGroupSelectionDropdownOpen = false;
        }
    }
    
    private void OnWindowPositionChanged(object sender, PixelPointEventArgs e)
    {
        // Close popup when window moves
        if (DataContext is ReportingPageViewModel viewModel)
        {
            viewModel.IsGroupSelectionDropdownOpen = false;
        }
    }
    
    // Your existing pointer event handlers
    private void OnGroupItemPointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = new SolidColorBrush(Color.Parse("#F3F4F6"));
        }
    }
    
    private void OnGroupItemPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = Brushes.Transparent;
        }
    }
    
    // In your View code-behind file (optional hover effects)

    private void OnItemPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = new SolidColorBrush(Color.Parse("#F3F4F6"));
        }
    }

    private void OnItemPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = Brushes.Transparent;
        }
    }
    public void ToggleSideMenu(object? sender, RoutedEventArgs routedEventArgs)
    {
        var viewModel = (ReportingPageViewModel)DataContext!;
        viewModel.IsMenuOpen = !viewModel.IsMenuOpen;
        AdjustGridColumns(viewModel.IsMenuOpen);
    }
    

// Alternative: If you want to apply the hover effect to the inner border instead
    private void OnGroupItemPointerEnteredAlt(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            // Find the inner border with background #F9FAFB
            var checkBox = border.Child as CheckBox;
            if (checkBox?.Content is Border innerBorder)
            {
                innerBorder.Background = new SolidColorBrush(Color.Parse("#EFF6FF"));
            }
        }
    }

    private void OnGroupItemPointerExitedAlt(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            var checkBox = border.Child as CheckBox;
            if (checkBox?.Content is Border innerBorder)
            {
                innerBorder.Background = new SolidColorBrush(Color.Parse("#F9FAFB"));
            }
        }
    }
    private void AdjustGridColumns(bool isMenuOpen)
    {
        // Access the grid directly and modify its column definitions
        var mainGrid = this.FindControl<Grid>("MainGrid");

        if (isMenuOpen)
        {
            // If the menu is open, set the width to an appropriate value (e.g., 250)
            mainGrid!.ColumnDefinitions[0].Width = new GridLength(3, GridUnitType.Star); // Main content column
            mainGrid.ColumnDefinitions[1].Width = new GridLength(420, GridUnitType.Pixel); // Side menu column
        }
        else
        {
            // Collapse the side menu column when it is closed
            mainGrid!.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star); // Main content column
            mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel); // Hide side menu column completely
        }
    }

    private async void ChooseFileButton_Click(object sender, RoutedEventArgs e)
    {
        var parentWindow = this.GetVisualRoot() as Window;

        if (parentWindow == null) return; // Ensure we have a valid window

        var openFileDialog = new OpenFileDialog
        {
            AllowMultiple = false,
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "text Files", Extensions = { "txt" } },
            }
        };

        var result = await openFileDialog.ShowAsync(parentWindow);

        if (result != null && result.Length > 0 && DataContext is ReportingPageViewModel viewModel)
        {
            viewModel.OnDropFile(result[0]);
        }
    }

    private async void SelectPathForSubjectFile(object sender, RoutedEventArgs e)
    {
        // Get the storage provider from the current window
        var storageProvider = ((Window)this.VisualRoot!)?.StorageProvider;

        if (storageProvider != null)
        {
            // Open the folder picker dialog
            var folderResult = await storageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Select a Folder"
            });

            if (folderResult.Count > 0)
            {
                // Get the selected folder path
                var folderPath = folderResult[0]?.Path.LocalPath;

                if (!string.IsNullOrEmpty(folderPath))
                {
                    if (DataContext is ReportingPageViewModel viewModel)
                    {
                        viewModel.PathToSaveSubjectFile = folderPath;
                    }
                }
            }
        }
    }
    
    private async void  SelectPathForCountFile(object sender, RoutedEventArgs e)
    {
        var storageProvider = ((Window)this.VisualRoot!)?.StorageProvider;

        if (storageProvider != null)
        {
            // Open the folder picker dialog
            var folderResult = await storageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Select a Folder"
            });

            if (folderResult.Count > 0)
            {
                // Get the selected folder path
                var folderPath = folderResult[0]?.Path.LocalPath;

                if (!string.IsNullOrEmpty(folderPath))
                {
                    if (DataContext is ReportingPageViewModel viewModel)
                    {
                        viewModel.PathToSaveCountFile = folderPath;
                    }
                }
            }
        }
    }
    private async void Border_OnDrop(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;

        // Check if any of the alternative formats contain file paths
        if (e.Data.Contains("FileName") || e.Data.Contains("FileNameW") || e.Data.Contains("Files"))
        {
            var files = e.Data.GetFileNames();

            if (files != null && DataContext is ReportingPageViewModel viewModel)
            {
                var filePath = files.First();
                if (IsValidFile(filePath))
                {
                    viewModel.OnDropFile(filePath);
                }
                else
                {
                    // Handle invalid file type feedback if needed
                }
            }
        }
    }


// Method to validate file extensions
    private bool IsValidFile(string filePath)
    {
        var validExtensions = new[] { ".txt" };
        var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

        return validExtensions.Contains(fileExtension);
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

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && DataContext is ReportingPageViewModel viewModel)
        {
            var selected = listBox.SelectedItems
                .OfType<KeyValuePair<string, string>>() // Ensure correct type
                .ToList();

            viewModel.SelectedFolders = new ObservableCollection<KeyValuePair<string, string>>(selected);
        }
    }

}