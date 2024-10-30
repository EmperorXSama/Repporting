using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RepportingApp.CoreSystem.Broadcast;

public class ProcessFinishedMessages : ValueChangedMessage<string>
{
    public string Type { get; }
    public string ProcessName { get; }
    public object Result { get; }

    public ProcessFinishedMessages(string type,string processName, object result)
        : base(processName)
    {
        this.Type = type;
        ProcessName = processName;
        Result = result;
    }
}