using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RepportingApp.ViewModels.ExtensionViewModel;

public class TaskInfoManager :INotifyPropertyChanged
{
    private readonly Dictionary<TaskCategory, ObservableCollection<TaskInfoUiModel?>> _taskCollections;
    public event PropertyChangedEventHandler? PropertyChanged;

    private ObservableCollection<TaskInfoUiModel> _activeTasks = new();
    private ObservableCollection<TaskInfoUiModel> _notificationTasks = new();
    private ObservableCollection<TaskInfoUiModel> _campaignTasks = new();
    public ObservableCollection<TaskInfoUiModel> ActiveTasks
    {
        get => _activeTasks;
        private set
        {
            _activeTasks = value;
            OnPropertyChanged(nameof(ActiveTasks));
        }
    }

    public int GetTasksCount()
    {
        return  _taskCollections[TaskCategory.Active].Count +  _taskCollections[TaskCategory.Campaign].Count;
    }
    public ObservableCollection<TaskInfoUiModel> NotificationTasks
    {
        get => _notificationTasks;
        private set
        {
            _notificationTasks = value;
            OnPropertyChanged(nameof(NotificationTasks));
        }
    }

    public ObservableCollection<TaskInfoUiModel> CampaignTasks
    {
        get => _campaignTasks;
        private set
        {
            _campaignTasks = value;
            OnPropertyChanged(nameof(CampaignTasks));
        }
    }
    public TaskInfoManager()
    {
        _taskCollections = new Dictionary<TaskCategory, ObservableCollection<TaskInfoUiModel?>>
        {
            { TaskCategory.Active, new ObservableCollection<TaskInfoUiModel?>() },
            { TaskCategory.Notification, new ObservableCollection<TaskInfoUiModel?>() },
            { TaskCategory.Campaign, new ObservableCollection<TaskInfoUiModel?>() }
        };
    }
    
    public ObservableCollection<TaskInfoUiModel?> GetTasks(TaskCategory category) => _taskCollections[category];
    
    public void AddTask(TaskCategory category, TaskInfoUiModel TaskInfoUiModel)
    {
        _taskCollections[category].Add(TaskInfoUiModel);
    }
    public void MoveTaskToCategory(Guid taskId, TaskCategory fromCategory, TaskCategory toCategory)
    {
        var TaskInfoUiModel = _taskCollections[fromCategory].FirstOrDefault(t => t.TaskId == taskId);
        if (TaskInfoUiModel != null)
        {
            TaskInfoUiModel.FinishedTime = DateTime.Now.ToString("g");
            _taskCollections[fromCategory].Remove(TaskInfoUiModel);
            _taskCollections[toCategory].Add(TaskInfoUiModel);
        }
    }
    public async void CompleteTask(Guid taskId, TaskCategory category = TaskCategory.Active)
    {
        TaskInfoUiModel? taskInfo =  GetTasks(TaskCategory.Active).FirstOrDefault(t => t.TaskId == taskId);
        if (taskInfo != null)
        {
            await DispatcherHelper.ExecuteOnUIThreadAsync( async () =>
            {
                taskInfo.FinishedTime = DateTime.UtcNow.ToString("t");
                SetIntervalWhenTaskFinished(taskInfo);
                if (taskInfo.AssignedGroup != null && taskInfo.AssignedGroup.Any())
                {
                    var itemsToRemove = taskInfo.AssignedGroup.Where(email => email.ApiResponses.Count < 2).ToList();

                    foreach (var email in itemsToRemove)
                    {
                        taskInfo.AssignedGroup.Remove(email);
                    }
                }
            });


        }
        MoveTaskToCategory(taskId, category, TaskCategory.Notification);
        
    }
    private void SetIntervalWhenTaskFinished(TaskInfoUiModel? taskInfo)
    {
        DateTime startTime = DateTime.Parse(taskInfo.StartTime);
        DateTime finishTime = DateTime.Parse(taskInfo.FinishedTime);
        taskInfo.Interval = finishTime - startTime;
    }
    public void ClearTasks(TaskCategory category) => _taskCollections[category].Clear();

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum TaskCategory
{
    Active,
    Notification,
    Campaign,
}