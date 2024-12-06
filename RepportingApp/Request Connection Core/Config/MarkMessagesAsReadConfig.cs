namespace RepportingApp.Request_Connection_Core.Config;

public partial class MarkMessagesAsReadConfig : ObservableObject
{
    [ObservableProperty] public int _minMessagesValue= 3;
     [ObservableProperty] public int _maxMessagesValue = 11;
     [ObservableProperty] public int _bulkThreshold = 60;
     [ObservableProperty] public int _bulkChunkSize = 30;
     [ObservableProperty] public int _singleThreshold = 20;
     [ObservableProperty] public string _directoryId = Statics.InboxDir;
     [ObservableProperty] public IEnumerable<FolderMessages>? _messages;
}