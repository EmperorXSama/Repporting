


namespace RepportingApp.ViewModels;

public partial class ProxyManagementPageViewModel : ViewModelBase,ILoadableViewModel
{
    private bool _isDataLoaded = false;
    [ObservableProperty]public ErrorIndicatorViewModel _errorIndicator= new ErrorIndicatorViewModel();

    public ProxyManagementPageViewModel(IMessenger messenger) : base(messenger)
    {
    }
    
    public async Task LoadDataIfFirstVisitAsync()
    {
        if (!_isDataLoaded)
        {
            _isDataLoaded = true;
            // Load data here
            await LoadDataAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        // Your data loading logic here
    }
    
    [RelayCommand]
    private async Task ProcessAStart()
    {
        _messenger.Send(new ProcessStartMessage("Success","Process B ",new ProcessModel()));
        await ShowIndicator("name", "something to show");
    }
    
    public async Task ShowIndicator(string title,string message)
    {
        try
        {
            ErrorIndicator = new ErrorIndicatorViewModel();
            await ErrorIndicator.ShowErrorIndecator(title, message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    
  
    }

}