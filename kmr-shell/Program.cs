using System.Text.Json;

namespace KMeleonR;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(args.Length > 0 ? args[0] : null));
    }
}

internal static class Settings
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "K-Meleon-R");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");
    private static Dictionary<string, string> data = Load();

    public static string ProfileDir => Path.Combine(Dir, "profile");

    private static Dictionary<string, string> Load()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath)) ?? new(); }
        catch { return new(); }
    }

    public static string Get(string key, string def) => data.TryGetValue(key, out var v) ? v : def;

    public static void Set(string key, string value)
    {
        data[key] = value;
        try { Directory.CreateDirectory(Dir); File.WriteAllText(FilePath, JsonSerializer.Serialize(data)); }
        catch { }
    }
}
