using System.Runtime.InteropServices;

namespace AutoBright;

internal static class DisplayBrightness
{
    private static T Query<T>(string query, Func<dynamic, T> action)
    {
        object locator = Activator.CreateInstance(Type.GetTypeFromProgID("WbemScripting.SWbemLocator")!)!;
        object? service = null, results = null;
        try
        {
            service = ((dynamic)locator).ConnectServer(".", @"root\WMI");
            results = ((dynamic)service).ExecQuery(query);
            return action(results);
        }
        finally
        {
            if (results != null) Marshal.FinalReleaseComObject(results);
            if (service != null) Marshal.FinalReleaseComObject(service);
            Marshal.FinalReleaseComObject(locator);
        }
    }

    internal static Dictionary<string, byte> Read() => Query("SELECT * FROM WmiMonitorBrightness WHERE Active = TRUE", rows =>
    {
        var values = new Dictionary<string, byte>();
        foreach (object row in rows)
        {
            try { values[Convert.ToString(Property(row, "InstanceName"))!] = Convert.ToByte(Property(row, "CurrentBrightness")); }
            finally { Marshal.FinalReleaseComObject(row); }
        }
        if (values.Count == 0) throw new NotSupportedException("No supported laptop display. This app requires Windows WMI brightness control; external monitors are not supported.");
        return values;
    });

    internal static void Set(Dictionary<string, byte> values)
    {
        Query("SELECT * FROM WmiMonitorBrightnessMethods WHERE Active = TRUE", rows =>
        {
            var remaining = values.Keys.ToHashSet();
            foreach (object row in rows)
            {
                try
                {
                    string id = Convert.ToString(Property(row, "InstanceName"))!;
                    if (!values.TryGetValue(id, out byte level)) continue;
                    SetPanel(row, level);
                    remaining.Remove(id);
                }
                finally { Marshal.FinalReleaseComObject(row); }
            }
            if (remaining.Count != 0) throw new IOException("A saved display is unavailable. Reconnect it and retry OFF to restore brightness.");
            return 0;
        });
        // This provider returns before the panel transition/read-back completes.
        // RunAsync already performs display work off the UI thread.
        Thread.Sleep(1500);
    }

    private static object Property(object instance, string name)
    {
        object properties = ((dynamic)instance).Properties_;
        object? property = null;
        try
        {
            property = ((dynamic)properties).Item(name);
            return ((dynamic)property).Value;
        }
        finally
        {
            if (property != null) Marshal.FinalReleaseComObject(property);
            Marshal.FinalReleaseComObject(properties);
        }
    }

    private static void SetPanel(object panel, byte level)
    {
        // WMI's scripting dispatch can expose parameter order differently from
        // the documented method signature. Bind by name using its input schema.
        var objects = new Stack<object>();
        object Keep(object value) { objects.Push(value); return value; }
        try
        {
            dynamic methods = Keep(((dynamic)panel).Methods_);
            dynamic method = Keep(methods.Item("WmiSetBrightness"));
            dynamic definition = Keep(method.InParameters);
            dynamic input = Keep(definition.SpawnInstance_());
            dynamic properties = Keep(input.Properties_);
            dynamic brightness = Keep(properties.Item("Brightness"));
            dynamic timeout = Keep(properties.Item("Timeout"));
            // WMI scripting represents uint32 using a signed Automation integer.
            brightness.Value = (int)level;
            timeout.Value = 0;
            object? result = ((dynamic)panel).ExecMethod_("WmiSetBrightness", input);
            if (result == null) return; // Void WMI methods can have no output object.
            dynamic output = Keep(result);
            dynamic outputProperties = Keep(output.Properties_);
            foreach (object property in outputProperties)
            {
                try
                {
                    dynamic value = property;
                    // Some drivers expose no return value. Check a numeric WMI
                    // status when present; Boolean provider outputs are not codes.
                    if ((string)value.Name == "ReturnValue" && value.Value is object status && status is not bool
                        && Convert.ToUInt32(status) != 0)
                        throw new IOException($"Windows rejected brightness adjustment ({status}).");
                }
                finally { Marshal.FinalReleaseComObject(property); }
            }
        }
        finally
        {
            while (objects.TryPop(out object? value)) Marshal.FinalReleaseComObject(value);
        }
    }
}
