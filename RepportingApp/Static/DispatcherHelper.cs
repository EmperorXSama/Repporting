namespace RepportingApp.Static;

public static class DispatcherHelper
{
    public static void ExecuteOnUIThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            // Already on the UI thread, invoke directly.
            action();
        }
        else
        {
            // Post to the UI thread.
            Dispatcher.UIThread.Post(action);
        }
    }
    public static async Task ExecuteOnUIThreadAsync(Func<Task> asyncAction)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            await asyncAction();
        }
        else
        {
            var tcs = new TaskCompletionSource();
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    await asyncAction();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            await tcs.Task;
        }
    }
}