using System.Diagnostics;
using System.Text.Json;

namespace Dairy.Web.Services;

public class PythonDbService
{
    public async Task<DbTestResult> TestConnectionAsync(string host, string database, string username, string password)
    {
        try
        {
            var scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Scripts", "db_connector.py");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\" \"{host}\" \"{database}\" \"{username}\" \"{password}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new DbTestResult { Success = false, Error = "Failed to start Python process" };
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return new DbTestResult { Success = false, Error = error };
            }

            var result = JsonSerializer.Deserialize<DbTestResult>(output, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new DbTestResult { Success = false, Error = "Invalid response from Python script" };
        }
        catch (Exception ex)
        {
            return new DbTestResult { Success = false, Error = ex.Message };
        }
    }
}

public class DbTestResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public string? Details { get; set; }
    public string? Version { get; set; }
}