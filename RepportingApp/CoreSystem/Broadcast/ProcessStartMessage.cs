using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RepportingApp.CoreSystem.Broadcast;

public class ProcessStartMessage : ValueChangedMessage<string>
{
    public string ProcessName { get; }
    public object Parameters { get; }
    public ProcessStartMessage(string processName ,object parameters) : base(processName)
    {
        ProcessName = processName;
        Parameters = parameters;
    }
}