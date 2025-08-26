using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Dairy.Web.Services;

public class DatabaseLoginModel : PageModel
{
    private readonly DatabaseSettingsService _settingsService;
    private readonly PythonDbService _pythonDbService;

    public DatabaseLoginModel(DatabaseSettingsService settingsService, PythonDbService pythonDbService)
    {
        _settingsService = settingsService;
        _pythonDbService = pythonDbService;
    }

    public string Host { get; set; } = "localhost";
    public string Database { get; set; } = "postgres";
    public string Username { get; set; } = "postgres";
    public bool RememberCredentials { get; set; } = true;
    public string ErrorMessage { get; set; } = "";

    public async Task OnGetAsync()
    {
        // Try auto-reconnect with saved settings
        var savedSettings = await _settingsService.LoadSettingsAsync();
        if (savedSettings != null && savedSettings.RememberCredentials)
        {
            try
            {
                var connectionString = _settingsService.BuildConnectionString(savedSettings);
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                
                HttpContext.Session.SetString("DatabaseConnected", "true");
                HttpContext.Session.SetString("ConnectionString", connectionString);
                Response.Redirect("/Dashboard");
                return;
            }
            catch
            {
                // Auto-reconnect failed, show form with saved values
                Host = savedSettings.Host;
                Database = savedSettings.Database;
                Username = savedSettings.Username;
                RememberCredentials = savedSettings.RememberCredentials;
            }
        }
        
        // Check if already connected
        if (HttpContext.Session.GetString("DatabaseConnected") == "true")
        {
            Response.Redirect("/Dashboard");
        }
    }

    public async Task<IActionResult> OnPostAsync(string host, string database, string username, string password, bool rememberCredentials = false)
    {
        try
        {
            var settings = new DatabaseSettings
            {
                Host = host,
                Database = database,
                Username = username,
                Password = password,
                RememberCredentials = rememberCredentials
            };
            
            var connectionString = _settingsService.BuildConnectionString(settings);
            
            // Test connection using Python script first
            var pythonResult = await _pythonDbService.TestConnectionAsync(host, database, username, password);
            if (!pythonResult.Success)
            {
                Host = host;
                Database = database;
                Username = username;
                RememberCredentials = rememberCredentials;
                ErrorMessage = $"Python test failed: {pythonResult.Error}";
                return Page();
            }
            
            // Test connection with .NET
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Save settings if requested
            if (rememberCredentials)
            {
                await _settingsService.SaveSettingsAsync(settings);
            }
            
            // Store connection info in session
            HttpContext.Session.SetString("DatabaseConnected", "true");
            HttpContext.Session.SetString("ConnectionString", connectionString);
            
            return RedirectToPage("/Dashboard");
        }
        catch (Exception ex)
        {
            Host = host;
            Database = database;
            Username = username;
            RememberCredentials = rememberCredentials;
            ErrorMessage = $"Connection failed: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostTestConnectionAsync(string host, string database, string username, string password)
    {
        var result = await _pythonDbService.TestConnectionAsync(host, database, username, password);
        return new JsonResult(result);
    }
}