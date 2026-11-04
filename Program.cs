namespace AutoBright;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args is ["--self-test"]) return SelfTests.Run();
            if (args is ["--test-camera"])
            {
                double light = CameraSample.ReadAsync("").GetAwaiter().GetResult();
                Console.WriteLine($"Camera capture succeeded; estimated light: {light:P1}. Display brightness unchanged.");
                return 0;
            }
            if (args is ["--test-display"])
            {
                var before = DisplayBrightness.Read();
                DisplayBrightness.Set(before);
                var after = DisplayBrightness.Read();
                if (before.Any(p => !after.TryGetValue(p.Key, out byte value) || value != p.Value))
                    throw new IOException("Display brightness read-back did not match the original value.");
                Console.WriteLine("Display brightness write/read-back succeeded at the existing brightness.");
                return 0;
            }
            if (args is ["--test-sample"])
            {
                var before = DisplayBrightness.Read();
                try
                {
                    double light = CameraSample.ReadAsync("").GetAwaiter().GetResult();
                    var targets = before.ToDictionary(p => p.Key, p => CameraSample.Target(light, p.Value));
                    DisplayBrightness.Set(targets);
                    var after = DisplayBrightness.Read();
                    Console.WriteLine($"Camera estimate: {light:P1}; brightness before: {before.Values.First()}%; target: {targets.Values.First()}%; read-back: {after.Values.First()}%.");
                    if (targets.Any(p => !after.TryGetValue(p.Key, out byte value) || value != p.Value))
                        throw new IOException("Display read-back did not match the requested brightness.");
                }
                finally { DisplayBrightness.Set(before); }
                var restored = DisplayBrightness.Read();
                if (before.Any(p => !restored.TryGetValue(p.Key, out byte value) || value != p.Value))
                    throw new IOException("Original brightness did not restore correctly.");
                Console.WriteLine("Camera-to-display sample completed and original brightness restored.");
                return 0;
            }
            if (args is ["--validate-task"]) { Scheduler.ValidateDefinition(); return 0; }
            if (args is ["--scheduled"]) { Automation.RunAsync("scheduled").GetAwaiter().GetResult(); return 0; }
            if (args is ["--disable"]) { Automation.RunAsync("off").GetAwaiter().GetResult(); return 0; }
            if (args is ["--render-ui", var directory])
            {
                ApplicationConfiguration.Initialize();
                Directory.CreateDirectory(directory);
                using var form = new Form1(true);
                form.Show();
                Application.DoEvents();
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                bitmap.Save(Path.Combine(directory, "AutoBright.png"));
                return 0;
            }
            if (args.Length != 0) return 2;
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            if (args.Length == 0) MessageBox.Show(ex.Message, "AutoBright needs attention");
            return 1;
        }
    }
}
