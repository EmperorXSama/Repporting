
namespace RepportingApp.CoreSystem.Multithread
{
    public class UnifiedTaskManager
    {
        // Core properties
        private readonly ConcurrentDictionary<Guid, Task> _activeTasks = new();
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokens;
        private readonly ConcurrentQueue<Func<Task>> _taskQueue;
        private readonly SemaphoreSlim _semaphore;

        // Events
        public event EventHandler<TaskCompletedEventArgs>? TaskCompleted;
        public event EventHandler<TaskErrorEventArgs>? TaskErrored;
        public event EventHandler<BatchCompletedEventArgs>? BatchCompleted;
        public event EventHandler<ItemProcessedEventArgs>? ItemProcessed;

        public UnifiedTaskManager(int maxDegreeOfParallelism)
        {
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
        public Guid StartBatch<T>(IEnumerable<T> items, Func<T, CancellationToken, Task> processFunc, int batchSize = 30)
        {
            var taskId = Guid.NewGuid();
            var cts = new CancellationTokenSource();
            _cancellationTokens[taskId] = cts;

            _taskQueue.Enqueue(() => ProcessBatch(taskId, items, processFunc, batchSize, cts.Token));
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

        // Processes a batch of items in chunks
        private async Task ProcessBatch<T>(Guid taskId, IEnumerable<T> items, Func<T, CancellationToken, Task> processFunc, int batchSize, CancellationToken cancellationToken)
        {
            var itemBatches = items.Batch(batchSize);
            foreach (var batch in itemBatches)
            {
                var batchTasks = batch.Select(async item =>
                {
                    try
                    {
                        await processFunc(item, cancellationToken);
                        ItemProcessed?.Invoke(this, new ItemProcessedEventArgs(taskId, item, null, success: true));
                    }
                    catch (OperationCanceledException)
                    {
                        // Handle item cancellation
                    }
                    catch (Exception ex)
                    {
                        ItemProcessed?.Invoke(this, new ItemProcessedEventArgs(taskId, item, ex, success: false));
                    }
                }).ToList();

                await Task.WhenAll(batchTasks);
                cancellationToken.ThrowIfCancellationRequested();
            }

            BatchCompleted?.Invoke(this, new BatchCompletedEventArgs(taskId));
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

        public BatchCompletedEventArgs(Guid taskId) => TaskId = taskId;
    }

    public class ItemProcessedEventArgs : EventArgs
    {
        public Guid TaskId { get; }
        public object Item { get; }
        public bool Success { get; }
        public Exception? Error { get; }

        public ItemProcessedEventArgs(Guid taskId, object item, Exception? error, bool success)
        {
            TaskId = taskId;
            Item = item;
            Success = success;
            Error = error;
        }
    }
}
