namespace AutoBright;

internal static class Automation
{
    internal static T Exclusive<T>(Func<T> action)
    {
        using var mutex = new Mutex(false, @"Local\AutoBright-" + Scheduler.UserSid);
        bool acquired;
        try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(45)); }
        catch (AbandonedMutexException) { acquired = true; }
        if (!acquired) throw new IOException("Another AutoBright sample is running. Try again shortly.");
        try { return action(); }
        finally { mutex.ReleaseMutex(); }
    }

    internal static Task<Preferences> RunAsync(string action, string cameraId = "") => Task.Run(() => Exclusive(() =>
    {
        var settings = Preferences.Load();
        string stage = "loading settings";
        try
        {
            if (action == "off")
            {
                settings.Enabled = false;
                settings.Save();
                stage = "removing the scheduled task";
                Scheduler.Remove();
                stage = "restoring display brightness";
                if (settings.PreviousBrightness != null) DisplayBrightness.Set(settings.PreviousBrightness);
                settings.PreviousBrightness = null;
            }
            else
            {
                if (action == "scheduled" && !settings.Enabled) return settings;
                if (action != "scheduled") settings.CameraId = cameraId;
                stage = "reading display brightness";
                var current = DisplayBrightness.Read();
                if (action == "on" && settings.PreviousBrightness == null)
                {
                    settings.PreviousBrightness = current;
                    settings.Save();
                }
                if (action == "on")
                {
                    stage = "registering the scheduled task";
                    Scheduler.Register(DateTimeOffset.UtcNow.AddMinutes(2));
                    settings.Enabled = true;
                    settings.Save();
                }
                stage = "sampling the camera";
                double light = CameraSample.ReadAsync(settings.CameraId).GetAwaiter().GetResult();
                var targets = current.ToDictionary(p => p.Key, p => CameraSample.Target(light, p.Value));
                stage = "setting display brightness";
                DisplayBrightness.Set(targets);
                stage = "reading back display brightness";
                var actual = DisplayBrightness.Read();
                settings.Light = light;
                settings.Brightness = actual.Values.First();
                settings.LastSample = DateTimeOffset.Now;
            }
            settings.LastError = null;
            settings.Save();
            return settings;
        }
        catch (Exception ex)
        {
            var failure = new IOException($"Failed while {stage} (0x{ex.HResult:X8}): {ex.Message}", ex);
            settings.LastError = failure.Message;
            settings.Save();
            throw failure;
        }
    }));
}
