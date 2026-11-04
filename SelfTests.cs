using System.Xml.Linq;

namespace AutoBright;

internal static class SelfTests
{
    internal static int Run()
    {
        int count = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("Failed: " + name);
            count++;
        }
        Check(CameraSample.Luminance([0, 0, 0, 255]) == 0, "black");
        Check(Math.Abs(CameraSample.Luminance([255, 255, 255, 255]) - 1) < .00001, "white");
        Check(Math.Abs(CameraSample.Luminance([0, 255, 0, 255]) - .7152) < .00001, "BGRA channel order");
        Check(CameraSample.Target(0, 50) == 35, "dark step limited");
        Check(CameraSample.Target(1, 50) == 65, "bright step limited");
        Check(CameraSample.Target(0, 20) == 20, "minimum");
        Check(CameraSample.Target(1, 100) == 85, "brightest scene dims gradually from 100");
        Check(CameraSample.Target(1, 84) == 84, "brightest scene target");
        Check(CameraSample.Target(.5625, 80) == 68, "former 80 percent target now 68");
        Check(CameraSample.Target(0, 23) == 23, "hysteresis");
        Check(StaWorker.RunAsync(async () =>
        {
            bool before = Thread.CurrentThread.GetApartmentState() == ApartmentState.STA;
            await Task.Delay(10);
            return before && Thread.CurrentThread.GetApartmentState() == ApartmentState.STA;
        }).GetAwaiter().GetResult(), "camera worker stays STA across await");
        try
        {
            StaWorker.RunAsync<int>(async () => { await Task.Yield(); throw new IOException("worker failure"); }).GetAwaiter().GetResult();
            Check(false, "worker propagates failure");
        }
        catch (IOException ex) { Check(ex.Message == "worker failure", "worker propagates failure"); }
        byte previous = 0;
        for (int i = 0; i <= 100; i++)
        {
            byte target = CameraSample.Target(i / 100d, 50);
            Check(target >= previous && target is >= 20 and <= 100, "monotonic bounded curve");
            previous = target;
        }
        string executable = @"C:\Apps & Tools\AutoBright.exe";
        var xml = XDocument.Parse(Scheduler.Definition("S-1-5-21-test", executable, DateTimeOffset.UtcNow.AddMinutes(2), DateTimeOffset.UtcNow));
        XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        Check(xml.Descendants(ns + "TimeTrigger").Single().Element(ns + "Repetition")?.Element(ns + "Interval")?.Value == "PT2M", "two-minute repetition");
        Check(xml.Descendants(ns + "Command").Single().Value == executable, "escaped executable");
        Check(xml.Descendants(ns + "RunLevel").Single().Value == "LeastPrivilege", "no elevation");
        Check(xml.Descendants(ns + "WakeToRun").Single().Value == "false", "no waking");
        Console.WriteLine($"{count} checks passed; camera, display, preferences and tasks untouched.");
        return 0;
    }
}
