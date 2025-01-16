using Avalonia.Interactivity;
using Avalonia.Media;

namespace RepportingApp.Views;

public partial class ProxyManagementPageView : UserControl
{
    public ProxyManagementPageView()
    {
        InitializeComponent(); ;
    }

    

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ProxyManagementPageViewModel viewModel && sender is DataGrid dataGrid)
        {
            viewModel.SelectedProxies = dataGrid.SelectedItems
                .OfType<CentralProxy>()
                .ToObservableCollection();
            viewModel.IsAllSelected = viewModel.CopyCentralProxyList.Count > 0 &&
                                      viewModel.SelectedProxies.Count == viewModel.CopyCentralProxyList.Count;
            if (viewModel.IsAllSelected)
            {
                SelectAllBorder.Background = Brushes.Azure;
                AllText.Text = "Deselect";
                AllText.Foreground = Brushes.Black;
                IconAll.Foreground = Brushes.Black;
            }
        }
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProxyManagementPageViewModel viewModel && sender is Button btn)
        {
            if (viewModel.IsAllSelected)
            {
                SelectAllBorder.Background = new SolidColorBrush(Color.Parse("#FFD85F")); 
                AllText.Text = "All";
                AllText.Foreground = new SolidColorBrush(Color.Parse("#303030")); 
                IconAll.Foreground = new SolidColorBrush(Color.Parse("#303030")); 
                DataGrid.SelectedItems.Clear();
            }
            else
            {
                SelectAllBorder.Background = Brushes.Azure;
                AllText.Text = "Deselect";
                AllText.Foreground = Brushes.Black;
                IconAll.Foreground = Brushes.Black;
                DataGrid.SelectAll();
            }
        }
      
    }

    /*private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ProxyManagementPageViewModel viewModel)
        {
            viewModel.ApplyFilter();
        }
    }*/
}