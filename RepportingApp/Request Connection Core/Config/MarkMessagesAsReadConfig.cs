namespace RepportingApp.Request_Connection_Core.Config;

public partial class MarkMessagesAsReadConfig : ObservableObject
{
     [ObservableProperty] public int _bulkThreshold = 60;
     [ObservableProperty] public int _bulkChunkSize = 30;
     [ObservableProperty] public int _singleThreshold = 20;
     [ObservableProperty] public string _directoryId = Statics.InboxDir;
     [ObservableProperty] public IEnumerable<FolderMessages>? _messages;
     [ObservableProperty] private PreReportingSettings _preReportingSettings = new PreReportingSettings();
}

public partial class PreReportingSettings : ObservableObject
{
    [ObservableProperty] private bool _isPreReporting = true;
    [ObservableProperty] public int _minMessagesToRead= 3;
    [ObservableProperty] public int _maxMessagesToRead = 11; 
    [ObservableProperty] public int _minMessagesToArchive= 0;
    [ObservableProperty] public int _maxMessagesToArchive = 0;
}