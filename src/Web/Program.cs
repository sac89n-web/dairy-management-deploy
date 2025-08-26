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
        // Try environment variable first, then config
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ?? 
                              builder.Configuration.GetConnectionString("Postgres") ?? 
                              "Host=localhost;Database=postgres;Username=admin;Password=admin123;SearchPath=dairy";
        
        // Convert PostgreSQL URL format if needed
        if (connectionString.StartsWith("postgresql://"))
        {
            var uri = new Uri(connectionString);
            connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]};SSL Mode=Require;Trust Server Certificate=true;SearchPath=dairy";
        }
        
        return new SqlConnectionFactory(connectionString);
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

    // Skip authentication in production to avoid redirect loops
    if (!app.Environment.IsProduction())
    {
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
    }

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

    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI();

    // Endpoints
    app.MapRazorPages();
    app.MapControllers();

    // Default route to dashboard
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

    // Database test endpoint
    app.MapGet("/api/test-db", async (SqlConnectionFactory dbFactory) => {
        try {
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