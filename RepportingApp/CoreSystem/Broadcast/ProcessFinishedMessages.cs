using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RepportingApp.CoreSystem.Broadcast;

public class ProcessFinishedMessages : ValueChangedMessage<string>
{
    public string ProcessName { get; }
    public object Result { get; }

    public ProcessFinishedMessages(string processName, object result)
        : base(processName)
    {
        ProcessName = processName;
        Result = result;
    }
}