using System.Text.Json;

public class Config
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "yt",
        "config.json");

    public static void Save(Config config)
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static Config? Load()
    {
        if (!File.Exists(ConfigPath)) return null;
        return JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigPath));
    }

    public static void Remove()
    {
        if (File.Exists(ConfigPath))
            File.Delete(ConfigPath);
    }

    public static Config LoadOrThrow()
    {
        var config = Load();
        if (config is null || string.IsNullOrEmpty(config.ApiKey))
        {
            Console.Error.WriteLine("Not authenticated. Run: yt auth <base-url> <api-key>");
            Environment.Exit(1);
        }
        return config!;
    }
}
