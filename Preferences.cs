using System.Text.Json;

namespace AutoBright;

internal sealed class Preferences
{
    public bool Enabled { get; set; }
    public string CameraId { get; set; } = "";
    public Dictionary<string, byte>? PreviousBrightness { get; set; }
    public DateTimeOffset? LastSample { get; set; }
    public double? Light { get; set; }
    public byte? Brightness { get; set; }
    public string? LastError { get; set; }
    internal static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoBright");
    internal static Preferences Load() => File.Exists(Path.Combine(DirectoryPath, "preferences.json"))
        ? JsonSerializer.Deserialize<Preferences>(File.ReadAllText(Path.Combine(DirectoryPath, "preferences.json"))) ?? throw new IOException("Preferences are empty.") : new();
    internal void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        string path = Path.Combine(DirectoryPath, "preferences.json");
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this));
        File.Move(temporary, path, true);
    }
}
