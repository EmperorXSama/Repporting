using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RepportingApp.ViewModels;

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


}