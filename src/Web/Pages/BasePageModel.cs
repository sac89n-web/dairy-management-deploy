using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;

public class BasePageModel : PageModel
{
    protected NpgsqlConnection GetConnection()
    {
        var sessionConnectionString = HttpContext.Session.GetString("ConnectionString");
        var connectionString = !string.IsNullOrEmpty(sessionConnectionString) 
            ? sessionConnectionString 
            : "Host=localhost;Database=postgres;Username=admin;Password=admin123;SearchPath=dairy";
            
        return new NpgsqlConnection(connectionString);
    }
}