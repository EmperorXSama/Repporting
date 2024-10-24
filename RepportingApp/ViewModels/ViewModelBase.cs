
using System;
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

    // Register common messages like ProcessStart and ProcessFinish
    protected virtual void RegisterForMessages()
    {
        _messenger.Register<ProcessStartMessage>(this, (r, m) =>
        {
            OnProcessStarted(m.ProcessName, m.Parameters);
        });

        _messenger.Register<ProcessFinishedMessages>(this, (r, m) =>
        {
            OnProcessFinished(m.ProcessName, m.Result);
        });
    }

    // Virtual methods to be overridden in child view models if needed
    protected virtual void OnProcessStarted(string processName, object parameters)
    {
        // Default implementation (optional), can be empty
    }

    protected virtual void OnProcessFinished(string processName, object result)
    {
        // Default implementation (optional), can be empty
    }

    // Clean up and unregister messages when the view model is disposed
    public void Dispose()
    {
        _messenger.UnregisterAll(this);
    }
}