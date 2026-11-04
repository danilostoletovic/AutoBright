namespace AutoBright;

// MediaCapture needs an STA with a dispatcher even during a scheduled execution.
// The dispatcher is hidden and exists only until the operation has cleaned up.
internal static class StaWorker
{
    internal static Task<T> RunAsync<T>(Func<Task<T>> operation)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                using var dispatcher = new Control();
                _ = dispatcher.Handle;
                using var context = new ApplicationContext();
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                dispatcher.BeginInvoke((Action)(async () =>
                {
                    try { completion.TrySetResult(await operation()); }
                    catch (Exception ex) { completion.TrySetException(ex); }
                    finally { context.ExitThread(); }
                }));
                Application.Run(context);
            }
            catch (Exception ex) { completion.TrySetException(ex); }
        }) { IsBackground = true, Name = "AutoBright camera" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
