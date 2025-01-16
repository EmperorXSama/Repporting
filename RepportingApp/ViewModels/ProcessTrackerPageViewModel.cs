namespace RepportingApp.ViewModels;

public partial class ProcessTrackerPageViewModel : ViewModelBase
{
    private readonly TaskInfoManager _taskInfoManager;
    [ObservableProperty] public TaskInfoUiModel? _selectedTask;
    public ProcessTrackerPageViewModel(IMessenger messenger,  TaskInfoManager taskInfoManager) : base(messenger)
    {
        _taskInfoManager = taskInfoManager;
    }
    
    public ObservableCollection<TaskInfoUiModel?> NotificationTasks =>
        _taskInfoManager.GetTasks(TaskCategory.Saved);
    
    [RelayCommand]
    private void SelectTask(TaskInfoUiModel? task)
    {
        SelectedTask = task;
    }
}