using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;
using Npgsql;

public class WeighingMachineModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public WeighingMachineModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<Farmer> Farmers { get; set; } = new();
    public List<Shift> Shifts { get; set; } = new();
    public List<WeighingRecentCollection> RecentCollections { get; set; } = new();

    public async Task OnGetAsync()
    {
        var sessionConnectionString = HttpContext.Session.GetString("ConnectionString");
        var connectionString = !string.IsNullOrEmpty(sessionConnectionString) 
            ? sessionConnectionString 
            : "Host=localhost;Database=postgres;Username=admin;Password=admin123;SearchPath=dairy";
            
        using var connection = GetConnection();
        
        Farmers = (await connection.QueryAsync<Farmer>(
            "SELECT id, name, code, contact FROM dairy.farmer ORDER BY name")).ToList();
        
        Shifts = (await connection.QueryAsync<Shift>(
            "SELECT id, name FROM dairy.shift ORDER BY id")).ToList();
        
        RecentCollections = (await connection.QueryAsync<WeighingRecentCollection>(@"
            SELECT mc.id, mc.qty_ltr, mc.fat_pct, mc.due_amt, mc.date as created_at, f.name as farmer_name, s.name as shift_name
            FROM dairy.milk_collection mc
            JOIN dairy.farmer f ON mc.farmer_id = f.id
            JOIN dairy.shift s ON mc.shift_id = s.id
            WHERE DATE(mc.date) = CURRENT_DATE
            ORDER BY mc.date DESC
            LIMIT 10")).ToList();
    }
}

public class WeighingRecentCollection
{
    public int id { get; set; }
    public string farmer_name { get; set; } = "";
    public string shift_name { get; set; } = "";
    public decimal qty_ltr { get; set; }
    public decimal fat_pct { get; set; }
    public decimal due_amt { get; set; }
    public DateTime created_at { get; set; }
}

