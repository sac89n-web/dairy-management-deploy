using System.Text.Json;

namespace Dairy.Web.Services;

public class DatabaseSettingsService
{
    private readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DairyManagement", "db-settings.json");

    public async Task SaveSettingsAsync(DatabaseSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    public async Task<DatabaseSettings?> LoadSettingsAsync()
    {
        if (!File.Exists(_settingsPath)) return null;
        
        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            return JsonSerializer.Deserialize<DatabaseSettings>(json);
        }
        catch
        {
            return null;
        }
    }

    public string BuildConnectionString(DatabaseSettings settings)
    {
        return $"Host={settings.Host};Database={settings.Database};Username={settings.Username};Password={settings.Password}";
    }
}

public class DatabaseSettings
{
    public string Host { get; set; } = "localhost";
    public string Database { get; set; } = "postgres";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = "";
    public bool RememberCredentials { get; set; } = true;
}