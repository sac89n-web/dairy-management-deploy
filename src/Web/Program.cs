using Serilog;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Serilog for structured logging
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .Enrich.WithProperty("RequestId", Guid.NewGuid())
    .MinimumLevel.Information());

// Essential services only
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// Database connection factory
builder.Services.AddSingleton<Func<NpgsqlConnection>>(sp =>
{
    var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrEmpty(dbUrl))
        throw new InvalidOperationException("DATABASE_URL environment variable is required");
    
    return () => new NpgsqlConnection(ParseDatabaseUrl(dbUrl));
});

var app = builder.Build();

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError("Unhandled exception occurred");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    });
});

// Request logging
app.UseSerilogRequestLogging();

// Health endpoints
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
}));

app.MapGet("/version", () => Results.Ok(new { 
    version = "1.0.0", 
    build = DateTime.UtcNow.ToString("yyyy-MM-dd")
}));

// Database connectivity test
app.MapGet("/db-test", async (Func<NpgsqlConnection> dbFactory, ILogger<Program> logger) =>
{
    try
    {
        using var conn = dbFactory();
        await conn.OpenAsync();
        
        using var cmd = new NpgsqlCommand("SELECT 1", conn);
        var result = await cmd.ExecuteScalarAsync();
        
        logger.LogInformation("Database test successful: {Result}", result);
        return Results.Ok(new { success = true, result });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database test failed");
        return Results.Problem($"Database error: {ex.Message}");
    }
});

// Startup database check
try
{
    var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(dbUrl))
    {
        using var conn = new NpgsqlConnection(ParseDatabaseUrl(dbUrl));
        await conn.OpenAsync();
        app.Logger.LogInformation("✅ Database connection verified at startup");
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "❌ Database connection failed at startup");
    // Don't throw - let app start for diagnostics
}

// CRITICAL: Bind to Render's PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Logger.LogInformation("🚀 Starting on port {Port}", port);
app.Run();

// Helper method to parse Render's DATABASE_URL
static string ParseDatabaseUrl(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    
    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}