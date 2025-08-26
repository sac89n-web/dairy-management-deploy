using Npgsql;

namespace Dairy.Web.Services;

public class DynamicConnectionFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DynamicConnectionFactory(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public NpgsqlConnection CreateConnection()
    {
        var connectionString = _httpContextAccessor.HttpContext?.Session.GetString("ConnectionString");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection not configured. Please login first.");
        }

        return new NpgsqlConnection(connectionString);
    }
}