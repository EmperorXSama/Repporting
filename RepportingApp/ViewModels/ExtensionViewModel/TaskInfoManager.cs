using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RepportingApp.ViewModels.ExtensionViewModel;

public class TaskInfoManager :INotifyPropertyChanged
{
    private readonly Dictionary<TaskCategory, ObservableCollection<TaskInfo>> _taskCollections;
    public event PropertyChangedEventHandler? PropertyChanged;

    private ObservableCollection<TaskInfo> _activeTasks = new();
    private ObservableCollection<TaskInfo> _notificationTasks = new();
    private ObservableCollection<TaskInfo> _campaignTasks = new();
    public ObservableCollection<TaskInfo> ActiveTasks
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
    public ObservableCollection<TaskInfo> NotificationTasks
    {
        get => _notificationTasks;
        private set
        {
            _notificationTasks = value;
            OnPropertyChanged(nameof(NotificationTasks));
        }
    }

    public ObservableCollection<TaskInfo> CampaignTasks
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
        _taskCollections = new Dictionary<TaskCategory, ObservableCollection<TaskInfo>>
        {
            { TaskCategory.Active, new ObservableCollection<TaskInfo>() },
            { TaskCategory.Notification, new ObservableCollection<TaskInfo>() },
            { TaskCategory.Campaign, new ObservableCollection<TaskInfo>() }
        };
    }
    
    public ObservableCollection<TaskInfo> GetTasks(TaskCategory category) => _taskCollections[category];
    
    public void AddTask(TaskCategory category, TaskInfo taskInfo)
    {
        _taskCollections[category].Add(taskInfo);
    }
    public void MoveTaskToCategory(Guid taskId, TaskCategory fromCategory, TaskCategory toCategory)
    {
        var taskInfo = _taskCollections[fromCategory].FirstOrDefault(t => t.TaskId == taskId);
        if (taskInfo != null)
        {
            taskInfo.FinishedTime = DateTime.Now.ToString("g");
            _taskCollections[fromCategory].Remove(taskInfo);
            _taskCollections[toCategory].Add(taskInfo);
        }
    }
    public void CompleteTask(Guid taskId, TaskCategory category = TaskCategory.Active)
    {
        MoveTaskToCategory(taskId, category, TaskCategory.Notification);
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