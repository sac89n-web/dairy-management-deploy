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

var app = builder.Build();

// Session middleware
app.UseSession();

// Auto-login for production
app.Use(async (context, next) =>
{
    var publicPaths = new[] { "/health", "/db-test", "/version" };
    
    if (!publicPaths.Any(p => context.Request.Path.StartsWithSegments(p)) && 
        context.Session.GetString("UserId") == null)
    {
        context.Session.SetString("UserId", "admin");
        context.Session.SetString("UserName", "Administrator");
    }
    
    await next();
});

// Localization Middleware
try
{
    var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
    app.UseRequestLocalization(locOptions);
}
catch { }

// Configure pipeline
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapRazorPages();
app.MapControllers();

// Default route to dashboard
app.MapGet("/", () => Results.Redirect("/dashboard"));

// Health endpoints
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
}));

app.MapGet("/db-test", async (SqlConnectionFactory dbFactory) => {
    try {
        using var connection = (NpgsqlConnection)dbFactory.CreateConnection();
        await connection.OpenAsync();
        
        using var cmd = new NpgsqlCommand("SELECT version()", connection);
        var version = await cmd.ExecuteScalarAsync();
        
        return Results.Json(new { 
            success = true, 
            message = "Connected successfully",
            version = version?.ToString()
        });
    } catch (Exception ex) {
        return Results.Json(new { success = false, error = ex.Message });
    }
});

app.MapGet("/setup-db", async (SqlConnectionFactory dbFactory) => {
    try {
        using var connection = (NpgsqlConnection)dbFactory.CreateConnection();
        await connection.OpenAsync();
        
        var setupSql = System.IO.File.ReadAllText("complete_schema.sql");
        
        using var cmd = new NpgsqlCommand(setupSql, connection);
        await cmd.ExecuteNonQueryAsync();
        
        return Results.Json(new { success = true, message = "Database setup completed" });
    } catch (Exception ex) {
        return Results.Json(new { success = false, error = ex.Message });
    }
});

// CRITICAL: Bind to Render's PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Logger.LogInformation("Starting Dairy Management System on port {Port}", port);
app.Run();