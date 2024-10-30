using System.Data.Common;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataAccess.Models;
using RepportingApp.CoreSystem.Broadcast;

namespace RepportingApp.ViewModels;

public partial class AutomationPageViewModel : ViewModelBase
{
    
    public AutomationPageViewModel(IMessenger messenger) : base(messenger)
    {
     
    }


    [RelayCommand]
    private async Task ProcessAStart()
    {
        _messenger.Send(new ProcessStartMessage("Success","Process A ",new ProcessModel()));
       
    }
    
    protected override async Task OnProcessStarted(string type,string processName, object parameters)
    {
      
    }

    protected override async Task OnProcessFinished(string type,string processName, object result)
    {
       
    }
    public void Dispose()
    {
        _messenger.UnregisterAll(this);
    }
}