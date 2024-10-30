

namespace RepportingApp.ViewModels;

public partial class ProxyManagementPageViewModel : ViewModelBase
{
    public ProxyManagementPageViewModel(IMessenger messenger) : base(messenger)
    {
    }
    
    
    
    [RelayCommand]
    private async Task ProcessAStart()
    {
        _messenger.Send(new ProcessStartMessage("Success","Process B ",new ProcessModel()));
       
    }
}