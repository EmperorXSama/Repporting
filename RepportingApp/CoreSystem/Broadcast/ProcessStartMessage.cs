using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RepportingApp.CoreSystem.Broadcast;

public class ProcessStartMessage : ValueChangedMessage<string>
{
    public string Type { get; }
    public string ProcessName { get; }
    public object Parameters { get; }
    public ProcessStartMessage(string type,string processName ,object parameters) : base(processName)
    {
        this.Type = type;
        ProcessName = processName;
        Parameters = parameters;
    }
}