using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using System.Text.Json;

public class TestDbModel : PageModel
{
    public string Result { get; set; } = "";
    public bool Success { get; set; } = false;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "dbconfig.json");
            if (!System.IO.File.Exists(configPath))
            {
                Result = "dbconfig.json not found";
                Success = false;
                return Page();
            }

            var configJson = await System.IO.File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
            
            var connectionString = $"Host={config["Host"]};Database={config["Database"]};Username={config["Username"]};Password={config["Password"]};Port={config.GetValueOrDefault("Port", 5432)}";
            
            Result = $"Testing connection:\nHost: {config["Host"]}\nDatabase: {config["Database"]}\nUsername: {config["Username"]}\nPassword: {new string('*', config["Password"].ToString().Length)}\nPort: {config.GetValueOrDefault("Port", 5432)}\n\n";
            
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            var cmd = new NpgsqlCommand("SELECT version()", connection);
            var version = await cmd.ExecuteScalarAsync();
            
            Result += $"SUCCESS: Connected to PostgreSQL\nVersion: {version}";
            Success = true;
        }
        catch (Exception ex)
        {
            Result += $"FAILED: {ex.Message}\n\nFull Exception:\n{ex}";
            Success = false;
        }
        
        return Page();
    }
}