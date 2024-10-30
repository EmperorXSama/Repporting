
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using RepportingApp.CoreSystem.Broadcast;

namespace RepportingApp.ViewModels;

public class ViewModelBase : ObservableObject,IDisposable
{
    
    protected readonly IMessenger _messenger;

    public ViewModelBase(IMessenger messenger)
    {
        _messenger = messenger;
        RegisterForMessages();
    }
    
    protected virtual async Task RegisterForMessages()
    {
        _messenger.Register<ProcessStartMessage>(this, (r, m) =>
        {
            _ = OnProcessStarted(m.Type,m.ProcessName, m.Parameters);
        });

        _messenger.Register<ProcessFinishedMessages>(this, (r, m) =>
        {
            _ = OnProcessFinished(m.Type,m.ProcessName, m.Result);
        });
    }

    
    protected virtual async Task OnProcessStarted(string type,string processName, object parameters)
    {
        //  base 
    }

    protected virtual async Task OnProcessFinished(string type,string processName, object result)
    {
        // base
    }

    
    public void Dispose()
    {
        _messenger.UnregisterAll(this);
    }
}