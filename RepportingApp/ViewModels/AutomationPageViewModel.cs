using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using RepportingApp.CoreSystem.Broadcast;

namespace RepportingApp.ViewModels;

public partial class AutomationPageViewModel(IMessenger messenger) : ViewModelBase(messenger)
{
    private readonly IMessenger _messenger = messenger;

    [ObservableProperty] private string _text = "Hello World";


    protected override void OnProcessStarted(string processName, object parameters)
    {
       Text = $"Process: {processName} started";
    }

    protected override void OnProcessFinished(string processName, object result)
    {
       Text = $"Process: {processName} finished";
    }
    public void Dispose()
    {
        _messenger.UnregisterAll(this);
    }
}