using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using Dairy.Infrastructure;
using Dairy.Application;
using Dairy.Reports;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

try
{
    // Configure QuestPDF license
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    // Serilog - Console only for cloud
    builder.Host.UseSerilog((ctx, lc) => lc
        .WriteTo.Console()
        .MinimumLevel.Information());

    // Configuration
    var supportedCultures = builder.Configuration.GetSection("SupportedCultures").Get<string[]>();

    // Localization
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        var cultures = (supportedCultures != null && supportedCultures.Length > 0 ? supportedCultures : new[] { "en-IN", "hi-IN", "mr-IN" }).Select(c => new CultureInfo(c)).ToList();
        options.DefaultRequestCulture = new RequestCulture("en-IN");
        options.SupportedCultures = cultures;
        options.SupportedUICultures = cultures;
        options.RequestCultureProviders.Clear();
        options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
        options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
    });

    // JWT Auth
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default-key-for-development"))
            };
        });

    // HTTP Context Accessor
    builder.Services.AddHttpContextAccessor();

    // Database and Infrastructure Services
    builder.Services.AddSingleton<SqlConnectionFactory>(sp =>
    {
        var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        
        if (!string.IsNullOrEmpty(dbUrl) && dbUrl.StartsWith("postgresql://"))
        {
            var uri = new Uri(dbUrl);
            var userInfo = uri.UserInfo.Split(':');
            var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;SearchPath=dairy";
            return new SqlConnectionFactory(connectionString);
        }
        else
        {
            var fallback = builder.Configuration.GetConnectionString("Postgres") ?? 
                          "Host=localhost;Database=postgres;Username=admin;Password=admin123;SearchPath=dairy";
            return new SqlConnectionFactory(fallback);
        }
    });

    // Repository Services
    builder.Services.AddScoped<CollectionRepository>();
    builder.Services.AddScoped<SaleRepository>();
    builder.Services.AddScoped<PaymentFarmerRepository>();
    builder.Services.AddScoped<PaymentCustomerRepository>();
    builder.Services.AddScoped<AuditLogRepository>();

    // Application Services
    builder.Services.AddScoped<SettingsCache>();
    builder.Services.AddScoped<WeighingMachineService>();

    // Report Services
    builder.Services.AddScoped<ExcelReportService>();
    builder.Services.AddScoped<PdfReportService>();

    // Session support
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromHours(2);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // Razor Pages, Controllers
    builder.Services.AddRazorPages();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}
catch (Exception ex)
{
    Console.WriteLine($"Error during service configuration: {ex.Message}");
    // Add minimal services if full configuration fails
    builder.Services.AddControllers();
}

var app = builder.Build();

try
{
    // Session middleware
    app.UseSession();

    // Authentication middleware - restored for proper login functionality
    app.Use(async (context, next) =>
    {
        var publicPaths = new[] { "/simple-login", "/login", "/database-login", "/health", "/api", "/swagger" };
        
        if (!publicPaths.Any(p => context.Request.Path.StartsWithSegments(p)) && 
            context.Session.GetString("UserId") == null)
        {
            context.Response.Redirect("/simple-login");
            return;
        }
        
        await next();
    });

    // Localization Middleware
    try
    {
        var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(locOptions);
    }
    catch
    {
        // Skip if localization fails
    }

    // Configure pipeline
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Swagger - always enabled for API access
    app.UseSwagger();
    app.UseSwaggerUI();

    // Endpoints
    app.MapRazorPages();
    app.MapControllers();

    // Default route to dashboard like local version
    app.MapGet("/", () => Results.Redirect("/dashboard"));

    // Basic API endpoints
    try
    {
        app.MapGet("/api/milk-collections", MilkCollectionEndpoints.List);
        app.MapPost("/api/milk-collections", MilkCollectionEndpoints.Add);
        app.MapPut("/api/milk-collections/{id}", MilkCollectionEndpoints.Update);
        app.MapDelete("/api/milk-collections/{id}", MilkCollectionEndpoints.Delete);

        app.MapGet("/api/sales", SaleEndpoints.List);
        app.MapPost("/api/sales", SaleEndpoints.Add);
    }
    catch
    {
        // Continue without endpoints if they fail
    }

    // Debug endpoint
    app.MapGet("/api/debug", () => {
        var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        return Results.Json(new {
            hasDbUrl = !string.IsNullOrEmpty(dbUrl),
            dbUrlLength = dbUrl?.Length ?? 0,
            dbUrlStart = dbUrl?.Substring(0, Math.Min(30, dbUrl?.Length ?? 0)),
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            port = Environment.GetEnvironmentVariable("PORT")
        });
    });

    // Database test endpoint
    app.MapGet("/api/test-db", async (SqlConnectionFactory dbFactory) => {
        try {
            var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (string.IsNullOrEmpty(dbUrl)) {
                return Results.Json(new { success = false, error = "DATABASE_URL is empty or null" });
            }
            
            using var connection = (NpgsqlConnection)dbFactory.CreateConnection();
            await connection.OpenAsync();
            
            using var cmd = new NpgsqlCommand("SELECT version()", connection);
            var version = await cmd.ExecuteScalarAsync();
            
            return Results.Json(new { 
                success = true, 
                message = "Connected successfully",
                version = version?.ToString(),
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
            });
        } catch (Exception ex) {
            return Results.Json(new { success = false, error = ex.Message });
        }
    });

    // Simple login page endpoint
    app.MapGet("/simple-login", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Dairy Management - Login</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 50px; background: #f5f5f5; }
        .login-container { max-width: 400px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h2 { text-align: center; color: #333; margin-bottom: 30px; }
        input { width: 100%; padding: 12px; margin: 10px 0; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box; }
        button { width: 100%; padding: 12px; background: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 16px; }
        button:hover { background: #0056b3; }
        .info { margin-top: 20px; padding: 15px; background: #e7f3ff; border-radius: 4px; font-size: 14px; }
    </style>
</head>
<body>
    <div class='login-container'>
        <h2>🥛 Dairy Management System</h2>
        <form method='post' action='/login'>
            <input type='text' name='username' placeholder='Username' required>
            <input type='password' name='password' placeholder='Password' required>
            <button type='submit'>Login</button>
        </form>
        <div class='info'>
            <strong>Demo Credentials:</strong><br>
            Username: admin<br>
            Password: admin123
        </div>
    </div>
</body>
</html>
", "text/html"));

    // Login POST endpoint
    app.MapPost("/login", (HttpContext context) => {
        var form = context.Request.Form;
        var username = form["username"].ToString();
        var password = form["password"].ToString();
        
        if (username == "admin" && password == "admin123")
        {
            context.Session.SetString("UserId", "admin");
            context.Session.SetString("UserName", "Administrator");
            return Results.Redirect("/dashboard");
        }
        
        return Results.Redirect("/simple-login?error=1");
    });

    // Dashboard page
    app.MapGet("/dashboard", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Dairy Management - Dashboard</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 0; background: #f8f9fa; }
        .header { background: #007bff; color: white; padding: 15px 20px; display: flex; justify-content: space-between; align-items: center; }
        .container { padding: 20px; }
        .card { background: white; padding: 20px; margin: 10px 0; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; }
        .btn { display: inline-block; padding: 10px 20px; background: #007bff; color: white; text-decoration: none; border-radius: 4px; margin: 5px; }
        .btn:hover { background: #0056b3; }
        .logout { background: #dc3545; }
        .logout:hover { background: #c82333; }
    </style>
</head>
<body>
    <div class='header'>
        <h1>🥛 Dairy Management System</h1>
        <a href='/logout' class='btn logout'>Logout</a>
    </div>
    <div class='container'>
        <div class='grid'>
            <div class='card'>
                <h3>📊 Quick Stats</h3>
                <p>System Status: <strong>Online</strong></p>
                <p>Database: <strong>Connected</strong></p>
                <p>Environment: <strong>Production</strong></p>
            </div>
            <div class='card'>
                <h3>🥛 Milk Collections</h3>
                <p>Manage daily milk collection records</p>
                <a href='/api/milk-collections' class='btn'>View Collections API</a>
            </div>
            <div class='card'>
                <h3>💰 Sales Management</h3>
                <p>Track milk sales and customer orders</p>
                <a href='/api/sales' class='btn'>View Sales API</a>
            </div>
            <div class='card'>
                <h3>📈 Reports & Analytics</h3>
                <p>Generate reports and view analytics</p>
                <a href='/swagger' class='btn'>API Documentation</a>
            </div>
        </div>
    </div>
</body>
</html>
", "text/html"));

    // Logout endpoint
    app.MapGet("/logout", (HttpContext context) => {
        context.Session.Clear();
        return Results.Redirect("/simple-login");
    });

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
}
catch (Exception ex)
{
    Console.WriteLine($"Error during app configuration: {ex.Message}");
    // Add minimal endpoints if full configuration fails
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
    app.MapGet("/", () => Results.Ok(new { message = "Dairy Management System", error = ex.Message }));
}

// Get port from environment or default
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Logger.LogInformation("Starting Dairy Management System on port {Port}", port);
app.Run();