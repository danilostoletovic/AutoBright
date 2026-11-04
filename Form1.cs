using Windows.Devices.Enumeration;

namespace AutoBright;

public partial class Form1 : Form
{
    private bool busy;
    private bool ready;
    private sealed record CameraOption(string Id, string Name)
    {
        public override string ToString() => Name;
    }

    public Form1() : this(false) { }

    internal Form1(bool preview)
    {
        InitializeComponent();
        using var iconStream = typeof(Form1).Assembly.GetManifestResourceStream("AutoBright.Assets.AutoBright.ico");
        if (iconStream != null) Icon = new Icon(iconStream);
        attentionLabel.Text = "Images stay in memory.\nCamera light turns on briefly.";
        if (preview)
        {
            cameras.Items.Add(new CameraOption("", "Default Windows camera"));
            cameras.SelectedIndex = 0;
            return;
        }
        Shown += async (_, _) =>
        {
            try
            {
                cameras.Items.Add(new CameraOption("", "Default Windows camera"));
                foreach (var device in await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture))
                    cameras.Items.Add(new CameraOption(device.Id, device.Name));
                var settings = await Task.Run(() => Automation.Exclusive(Preferences.Load));
                cameras.SelectedIndex = 0;
                for (int i = 1; i < cameras.Items.Count; i++)
                    if (cameras.Items[i] is CameraOption option && option.Id == settings.CameraId) cameras.SelectedIndex = i;
                ready = true;
                await RefreshStateAsync();
            }
            catch (Exception ex) { attentionLabel.Text = ex.Message; }
        };
        FormClosing += (_, e) => { if (busy) e.Cancel = true; };
    }

    private async Task RefreshStateAsync()
    {
        var state = await Task.Run(() => Automation.Exclusive(() => (Settings: Preferences.Load(), Exists: Scheduler.Exists())));
        var settings = state.Settings;
        toggleButton.Text = settings.Enabled ? "ON" : settings.PreviousBrightness != null || state.Exists ? "OFF · Clean up" : "OFF";
        statusLabel.Text = settings.Enabled ? "Automatic brightness enabled" : "Automatic brightness disabled";
        nextLabel.Text = settings.LastSample is { } time
            ? $"Last: {time.LocalDateTime:ddd, d MMM · HH:mm} · {settings.Brightness}%\nCamera light estimate: {settings.Light:P0}"
            : "Camera-based brightness. Every two minutes.";
        attentionLabel.Text = settings.LastError ?? (settings.Enabled && !state.Exists
            ? "Schedule missing. Turn OFF, then ON to repair."
            : "Images stay in memory. Camera light turns on briefly.\nEnabled sampling continues after closing the app.");
        cameras.Enabled = !settings.Enabled && !busy;
    }

    private async void ToggleButton_Click(object? sender, EventArgs e)
    {
        await ActAsync(null);
    }

    private async void SampleLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        await ActAsync("sample");
    }

    private async Task ActAsync(string? action)
    {
        if (busy || !ready) return;
        busy = true;
        toggleButton.Enabled = locationLink.Enabled = cameras.Enabled = false;
        UseWaitCursor = true;
        try
        {
            if (action == null)
            {
                var state = await Task.Run(() => Automation.Exclusive(() => (Settings: Preferences.Load(), Exists: Scheduler.Exists())));
                action = state.Settings.Enabled || state.Settings.PreviousBrightness != null || state.Exists ? "off" : "on";
            }
            await Automation.RunAsync(action, (cameras.SelectedItem as CameraOption)?.Id ?? "");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message + "\n\nCheck Settings > Privacy & security > Camera (including desktop apps), the camera shutter, and other apps using the camera.",
                "AutoBright needs attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            busy = false;
            toggleButton.Enabled = locationLink.Enabled = true;
            UseWaitCursor = false;
            try { await RefreshStateAsync(); }
            catch (Exception ex) { attentionLabel.Text = ex.Message; }
        }
    }
}
