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
using ReportingPageViewModel = RepportingApp.ViewModels.ReportingPageViewModel;

namespace RepportingApp.Views;

public partial class ReportingPageView : UserControl
{
    public ReportingPageView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ReportingPageViewModel>();
    }
    
    
    
    public void ToggleSideMenu(object? sender, RoutedEventArgs routedEventArgs)
    {
        var viewModel = (ReportingPageViewModel)DataContext!;
        viewModel.IsMenuOpen = !viewModel.IsMenuOpen;
        AdjustGridColumns(viewModel.IsMenuOpen);
    }
    
    private void AdjustGridColumns(bool isMenuOpen)
    {
        // Access the grid directly and modify its column definitions
        var mainGrid = this.FindControl<Grid>("MainGrid");

        if (isMenuOpen)
        {
            // If the menu is open, set the width to an appropriate value (e.g., 250)
            mainGrid!.ColumnDefinitions[0].Width = new GridLength(3, GridUnitType.Star); // Main content column
            mainGrid.ColumnDefinitions[1].Width = new GridLength(320, GridUnitType.Pixel); // Side menu column
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
                new FileDialogFilter { Name = "Excel Files", Extensions = { "xls", "xlsx" } },
                new FileDialogFilter { Name = "text Files", Extensions = { "txt" } },
            }
        };

        var result = await openFileDialog.ShowAsync(parentWindow);

        if (result != null && result.Length > 0 && DataContext is ReportingPageViewModel viewModel)
        {
            viewModel.OnDropFile(result[0]);
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
        var validExtensions = new[] { ".xlsx", ".xls", ".txt" };
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
}