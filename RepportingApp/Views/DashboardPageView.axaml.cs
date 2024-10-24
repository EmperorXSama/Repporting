using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using RepportingApp.ViewModels;

namespace RepportingApp.Views
{
    public partial class DashboardPageView : UserControl
    {
        public DashboardPageView()
        {
            InitializeComponent();
            DataContext = App.Services.GetRequiredService<DashboardPageViewModel>();
        }

        private void DashboardPageView_DataContextChanged(object sender, EventArgs e)
        {
            // Subscribe to the IsMenuOpen property change here if using INotifyPropertyChanged
        }

        public void ToggleSideMenu(object? sender, RoutedEventArgs routedEventArgs)
        {
            // Assuming IsMenuOpen is a property in your ViewModel
            var viewModel = (DashboardPageViewModel)DataContext;

            if (viewModel != null)
            {
                // Toggle the IsMenuOpen property directly
                viewModel.IsMenuOpen = !viewModel.IsMenuOpen;

                // Adjust the grid columns based on the side menu state
                AdjustGridColumns(viewModel.IsMenuOpen);
            }
        }

        private void AdjustGridColumns(bool isMenuOpen)
        {
            // Access the grid directly and modify its column definitions
            var mainGrid = this.FindControl<Grid>("MainGrid");

            if (isMenuOpen)
            {
                // If the menu is open, set the width to an appropriate value (e.g., 250)
                mainGrid.ColumnDefinitions[0].Width = new GridLength(3, GridUnitType.Star); // Main content column
                mainGrid.ColumnDefinitions[1].Width = new GridLength(250, GridUnitType.Pixel); // Side menu column
            }
            else
            {
                // Collapse the side menu column when it is closed
                mainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star); // Main content column
                mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel); // Hide side menu column completely
            }
        }

    }
}