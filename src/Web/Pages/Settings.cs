using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Web.Services;
using Npgsql;

public class SettingsModel : PageModel
{
    private readonly DatabaseSettingsService _dbSettingsService;

    public SettingsModel(DatabaseSettingsService dbSettingsService)
    {
        _dbSettingsService = dbSettingsService;
    }

    [BindProperty]
    public string SystemName { get; set; } = "Dairy Management System";
    [BindProperty]
    public string Contact { get; set; } = "";
    [BindProperty]
    public string Address { get; set; } = "";
    
    public string DbHost { get; set; } = "localhost";
    public string DbDatabase { get; set; } = "postgres";
    public string DbUsername { get; set; } = "postgres";
    public bool RememberCredentials { get; set; } = true;
    public string DatabaseMessage { get; set; } = "";

    public async Task OnGetAsync()
    {
        // Load system settings
        SystemName = "Dairy Management System";
        Contact = "9876543210";
        Address = "123 Dairy Lane";
        
        // Load database settings
        var dbSettings = await _dbSettingsService.LoadSettingsAsync();
        if (dbSettings != null)
        {
            DbHost = dbSettings.Host;
            DbDatabase = dbSettings.Database;
            DbUsername = dbSettings.Username;
            RememberCredentials = dbSettings.RememberCredentials;
        }
    }

    public async Task<IActionResult> OnPostSystemSettingsAsync()
    {
        // Save system settings logic here
        return Page();
    }

    public async Task<IActionResult> OnPostDatabaseSettingsAsync(string host, string database, string username, string password, bool rememberCredentials = false)
    {
        try
        {
            var settings = new DatabaseSettings
            {
                Host = host,
                Database = database,
                Username = username,
                Password = string.IsNullOrEmpty(password) ? (await _dbSettingsService.LoadSettingsAsync())?.Password ?? "" : password,
                RememberCredentials = rememberCredentials
            };
            
            var connectionString = _dbSettingsService.BuildConnectionString(settings);
            
            // Test connection
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Save settings
            await _dbSettingsService.SaveSettingsAsync(settings);
            
            // Update session
            HttpContext.Session.SetString("ConnectionString", connectionString);
            
            DatabaseMessage = "Database connection tested and saved successfully!";
        }
        catch (Exception ex)
        {
            DatabaseMessage = $"Connection failed: {ex.Message}";
        }
        
        DbHost = host;
        DbDatabase = database;
        DbUsername = username;
        RememberCredentials = rememberCredentials;
        
        return Page();
    }

    public async Task<IActionResult> OnPostClearCredentialsAsync()
    {
        var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DairyManagement", "db-settings.json");
        if (System.IO.File.Exists(settingsPath))
        {
            System.IO.File.Delete(settingsPath);
        }
        return new JsonResult(new { success = true });
    }
}