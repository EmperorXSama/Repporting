
using Avalonia.Threading;
using RepportingApp.ViewModels.ExtensionViewModel;
using TaskStatus = RepportingApp.Models.UI.TaskStatus;

namespace RepportingApp.CoreSystem.Multithread
{
    public class UnifiedTaskManager
    {
        // Core properties
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokens;
        private readonly ConcurrentQueue<Func<Task>> _taskQueue;
        private readonly SemaphoreSlim _semaphore;
        private readonly TaskInfoManager _taskInfoManager;
        // Events
        public event EventHandler<TaskCompletedEventArgs>? TaskCompleted;
        public event EventHandler<TaskErrorEventArgs>? TaskErrored;
        public event EventHandler<BatchCompletedEventArgs>? BatchCompleted;
        public event EventHandler<ItemProcessedEventArgs>? ItemProcessed;

        public UnifiedTaskManager(int maxDegreeOfParallelism, TaskInfoManager taskInfoManager)
        {
            _taskInfoManager = taskInfoManager;
            _semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            _taskQueue = new ConcurrentQueue<Func<Task>>();
            _cancellationTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
        }

        // Start a single task
        public Guid StartTask(Func<CancellationToken, Task> taskFunc)
        {
            var taskId = Guid.NewGuid();
            var cts = new CancellationTokenSource();
            _cancellationTokens[taskId] = cts;

            _taskQueue.Enqueue(() => ProcessTask(taskId, taskFunc, cts.Token));
            TryDequeueTask();

            return taskId;
        }

        // Start a batch of tasks with specified batch size
        public Guid StartBatch<T>(IEnumerable<T>? items, Func<T, CancellationToken, Task<string>> processFunc,TaskCategory taskCategory = TaskCategory.Active, int batchSize = 30)
        {
            var taskId = Guid.NewGuid();
            var cts = new CancellationTokenSource();
            _cancellationTokens[taskId] = cts;

            _taskQueue.Enqueue(() => ProcessBatch(taskId, items, processFunc, batchSize,taskCategory, cts.Token));
            TryDequeueTask();

            return taskId;
        }



        // Attempts to dequeue a task from the queue
        private void TryDequeueTask()
        {
            if (_taskQueue.TryDequeue(out var taskFunc))
            {
                Task.Run(async () =>
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        await taskFunc();
                    }
                    finally
                    {
                        _semaphore.Release();
                        TryDequeueTask(); // Continue processing the next task in the queue
                    }
                });
            }
        }

        // Processes a single task
        private async Task ProcessTask(Guid taskId, Func<CancellationToken, Task> taskFunc, CancellationToken cancellationToken)
        {
            try
            {
                await taskFunc(cancellationToken);
                TaskCompleted?.Invoke(this, new TaskCompletedEventArgs(taskId));
            }
            catch (OperationCanceledException)
            {
                // Handle task cancellation
                TaskErrored?.Invoke(this, new TaskErrorEventArgs(taskId, new TaskCanceledException("Task was cancelled")));
            }
            catch (Exception ex)
            {
                TaskErrored?.Invoke(this, new TaskErrorEventArgs(taskId, ex));
            }
            finally
            {
                _cancellationTokens.TryRemove(taskId, out _);
            }
        }

 
        private async Task ProcessBatch<T>(
            Guid taskId,
            IEnumerable<T>? items,
            Func<T, CancellationToken, Task<string>> processFunc,
            int batchSize,
            TaskCategory taskCategory,
            CancellationToken cancellationToken)
        {
            try
            {

                if (taskCategory != TaskCategory.Invincible)
                {
                    var taskInfo = await WaitForTaskInfo(taskId, taskCategory, TimeSpan.FromSeconds(10));
                    if (taskInfo == null)
                    {
                        // Handle the case where the task is not added within the timeout
                        throw new InvalidOperationException("TaskInfo was not added to the collection in time.");
                    }
                }
             
                var allProcessedItems = new List<object>(); // Collect all processed items
                var itemBatches = items.Batch(batchSize);
                
                //Random random = new Random();
               
                foreach (var batch in itemBatches)
                {
                    //var itemBatches2 = batch.ToList();
                    cancellationToken.ThrowIfCancellationRequested(); 
                    //var y =  random.Next(1000, 9999);
                    //itemBatches2.WriteListLine($"EmaulAcc{y}.txt");
                    var batchTasks = batch.Select(async item =>
                    {
                        try
                        {
                            string result = await processFunc(item, cancellationToken);
                            allProcessedItems.Add(item); // Collect successfully processed item
                            ItemProcessed?.Invoke(this, new ItemProcessedEventArgs(taskId, item, null, success: true, result, category: taskCategory));
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            ItemProcessed?.Invoke(this, new ItemProcessedEventArgs(taskId, item, ex, success: false, ex.Message,category: taskCategory));
                        }
                    }).ToList();
                 
                    await Task.WhenAll(batchTasks); // Wait for the current batch to complete
                    BatchCompleted?.Invoke(this, new BatchCompletedEventArgs(taskId, allProcessedItems));
                    await Task.Delay(5, cancellationToken);
                }
                
            }
            catch (OperationCanceledException)
            {
                // Handle batch-level cancellation
                TaskErrored?.Invoke(this, new TaskErrorEventArgs(taskId, new TaskCanceledException("Batch process was cancelled")));
            }
            catch (Exception ex)
            {
                TaskErrored?.Invoke(this, new TaskErrorEventArgs(taskId, ex));
            }
            finally
            {
                _cancellationTokens.TryRemove(taskId, out _);
            }
            TaskCompleted?.Invoke(this, new TaskCompletedEventArgs(taskId));
        }
        private async Task<TaskInfoUiModel?> WaitForTaskInfo(Guid taskId, TaskCategory category, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < timeout)
            {
                var taskInfo = _taskInfoManager.GetTasks(category).FirstOrDefault(t => t.TaskId == taskId);
                if (taskInfo != null)
                    return taskInfo;

                await Task.Delay(50); // Check every 50ms
            }
            return null;
        }

        // Process a looping batch of tasks with specified interval
        private async Task ProcessLoopingTaskBatch<T>(
    Guid taskId,
    IEnumerable<T> items,
    Func<T, CancellationToken, Task<string>> processFunc,
    int batchSize,
    TaskCategory category,
    Func<DateTime> getNextScheduledTime,
    CancellationToken cancellationToken)
{
    try
    {
        var taskInfo = await WaitForTaskInfo(taskId, TaskCategory.Campaign, TimeSpan.FromSeconds(5));
        if (taskInfo == null)
            throw new InvalidOperationException("TaskInfo was not added to the collection in time.");

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var now = DateTime.Now;
            var nextRunTime = getNextScheduledTime();

            if (nextRunTime > now)
            {
                taskInfo.ItemSuccesMessasges.Clear();
                taskInfo.WorkingStatus = TaskStatus.Waiting;
                var delay = nextRunTime - now;
                while (delay > TimeSpan.Zero)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    delay = nextRunTime - DateTime.Now;
                    taskInfo.TimeUntilNextRun = delay;
                }
            }
            taskInfo.WorkingStatus = TaskStatus.Running;
            var itemBatches = items.Batch(batchSize);
            foreach (var batch in itemBatches)
            {
                var batchTasks = batch.Select(async item =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string result = await processFunc(item, cancellationToken);
                        ItemProcessed?.Invoke(this, new ItemProcessedEventArgs(taskId, item, null, true, result, category));
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        ItemProcessed?.Invoke(this, new ItemProcessedEventArgs(taskId, item, ex, false, null, category));
                    }
                }).ToList();

                await Task.WhenAll(batchTasks);
                BatchCompleted?.Invoke(this, new BatchCompletedEventArgs(taskId, null));
            }
        }
    }
    catch (OperationCanceledException)
    {
        TaskErrored?.Invoke(this, new TaskErrorEventArgs(taskId, new TaskCanceledException("Looping batch process was cancelled")));
    }
    catch (Exception ex)
    {
        TaskErrored?.Invoke(this, new TaskErrorEventArgs(taskId, ex));
    }
    finally
    {
        _cancellationTokens.TryRemove(taskId, out _);
    }

    TaskCompleted?.Invoke(this, new TaskCompletedEventArgs(taskId));
}

        public Guid StartLoopingTask(Func<CancellationToken, Task> taskFunc, TimeSpan interval)
        {
            var taskId = Guid.NewGuid();
            var cts = new CancellationTokenSource();
            _cancellationTokens[taskId] = cts;
            
            _taskQueue.Enqueue(() => ProcessLoopingTask(taskId, taskFunc, interval, cts.Token));
            TryDequeueTask();
            return taskId;
        }
        // New StartLoopingTaskBatch method
        public Guid StartLoopingTaskBatch<T>(
            IEnumerable<T> items,
            Func<T, CancellationToken, Task<string>> processFunc,
            int batchSize,
            TaskCategory taskCategory,
            Func<DateTime> getNextScheduledTime)
        {
            var taskId = Guid.NewGuid();
            var cts = new CancellationTokenSource();
            _cancellationTokens[taskId] = cts;

            _taskQueue.Enqueue(() =>
                ProcessLoopingTaskBatch(taskId, items, processFunc, batchSize, taskCategory, getNextScheduledTime, cts.Token));

            TryDequeueTask();
            return taskId;
        }


        private async Task ProcessLoopingTask(Guid taskId, Func<CancellationToken, Task> taskFunc, TimeSpan interval, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    DateTime nextRunTime = DateTime.Now + interval;
                    var taskInfo = _taskInfoManager.GetTasks(TaskCategory.Campaign).FirstOrDefault(t => t.TaskId == taskId);
                    UpdateUiThreadValues(()=>
                    {
                        if (taskInfo != null) taskInfo.WorkingStatus = TaskStatus.Waiting;
                    });
                    // Update remaining time every second
                    while (DateTime.Now < nextRunTime)
                    {
                        var remainingTime = nextRunTime - DateTime.Now;
                
                        // Use Dispatcher to update the UI-bound TaskInfo object
                        UpdateUiThreadValues(() =>
                        {
                            if (taskInfo != null)
                            {
                                taskInfo.TimeUntilNextRun = remainingTime;
                            }
                        });
                       
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // Update every second
                    }
                    
                    /*
                    var relativePath = Path.Combine("Assets", "SFX", "UiSfx.mp3");
                    using var audioFile = new AudioFileReader(relativePath);
                    using var outputDevice = new WaveOutEvent();
                    outputDevice.Volume = 0.1f;
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    */
                    
                    
                    UpdateUiThreadValues(()=>
                    {
                        if (taskInfo != null) taskInfo.WorkingStatus = TaskStatus.Running;
                    });
                    await taskFunc(cancellationToken); 
                    TaskCompleted?.Invoke(this, new TaskCompletedEventArgs(taskId));
                }
            }
            catch (OperationCanceledException)
            {
                TaskErrored?.Invoke(this, new TaskErrorEventArgs(taskId, new TaskCanceledException("Looping task was cancelled")));
            }
            catch (Exception ex)
            {
                TaskErrored?.Invoke(this, new TaskErrorEventArgs(taskId, ex));
            }
            finally
            {
                _cancellationTokens.TryRemove(taskId, out _);
            }
        }
        // update variables from thread to UI 
        public void UpdateUiThreadValues(params Action[] actions)
        {
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var action in actions)
                    action();

            });
        }

        // Cancels a single task
        public void CancelTask(Guid taskId)
        {
            if (_cancellationTokens.TryRemove(taskId, out var cts))
            {
                cts.Cancel();
            }
        }

        // Cancels multiple tasks by their task IDs
        public void CancelTasks(IEnumerable<Guid> taskIds)
        {
            foreach (var taskId in taskIds)
            {
                CancelTask(taskId);
            }
        }
        
        public async Task WaitForTaskCompletion(Guid taskId)
        {
            // Wait for the task to finish
            while (_cancellationTokens.ContainsKey(taskId))
            {
                // This can be improved with an event or other signaling mechanism
                await Task.Delay(100); // Short delay before checking again
            }
        }
    }

    // Event argument classes
    public class TaskCompletedEventArgs : EventArgs
    {
        public Guid TaskId { get; }

        public TaskCompletedEventArgs(Guid taskId) => TaskId = taskId;
    }

    public class TaskErrorEventArgs : EventArgs
    {
        public Guid TaskId { get; }
        public Exception Error { get; }

        public TaskErrorEventArgs(Guid taskId, Exception error)
        {
            TaskId = taskId;
            Error = error;
        }
    }

    public class BatchCompletedEventArgs : EventArgs
    {
        public Guid TaskId { get; }
        public IEnumerable<object>? ProcessedItems { get; }

        public BatchCompletedEventArgs(Guid taskId, IEnumerable<object>? processedItems)
        {
            TaskId = taskId;
            ProcessedItems = processedItems;
        }
    }

    public class ItemProcessedEventArgs : EventArgs
    {
        public Guid TaskId { get; }
        public TaskCategory TaskCategoryName{ get; }
        public object Item { get; }
        public bool Success { get; }
        public Exception? Error { get; }
        public string? Message { get; }

        public ItemProcessedEventArgs(Guid taskId, object item, Exception? error, bool success,string message,TaskCategory category)
        {
            TaskId = taskId;
            TaskCategoryName = category;
            Item = item;
            Success = success;
            Error = error;
            Message = message;
        }
    }
}
