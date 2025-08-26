using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class RoutesModel : PageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public RoutesModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<Route> Routes { get; set; } = new();
    public List<RouteSummary> RouteSummaries { get; set; } = new();

    public async Task OnGetAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        
        Routes = (await connection.QueryAsync<Route>(@"
            SELECT r.*, COUNT(rf.farmer_id) as farmer_count
            FROM dairy.routes r
            LEFT JOIN dairy.route_farmers rf ON r.id = rf.route_id
            GROUP BY r.id, r.name, r.driver_name, r.vehicle_number, r.status, r.total_distance
            ORDER BY r.name")).ToList();

        var today = DateTime.Today;
        RouteSummaries = (await connection.QueryAsync<RouteSummary>(@"
            SELECT r.id as route_id, r.name as route_name, r.status,
                   COUNT(mc.id) as collection_count,
                   COALESCE(SUM(mc.qty_ltr), 0) as total_quantity
            FROM dairy.routes r
            LEFT JOIN dairy.route_farmers rf ON r.id = rf.route_id
            LEFT JOIN dairy.milk_collection mc ON rf.farmer_id = mc.farmer_id AND DATE(mc.date) = @today
            GROUP BY r.id, r.name, r.status
            ORDER BY r.name", new { today })).ToList();
    }

    public async Task<IActionResult> OnPostAddRouteAsync(string name, string driverName, string vehicleNumber)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO dairy.routes (name, driver_name, vehicle_number, status, total_distance)
            VALUES (@name, @driverName, @vehicleNumber, 'Active', 0)",
            new { name, driverName, vehicleNumber });
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStartRouteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            UPDATE dairy.routes SET status = 'In Progress', started_at = @now WHERE id = @id",
            new { id, now = DateTime.Now });
        
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        // Delete route farmers first
        await connection.ExecuteAsync("DELETE FROM dairy.route_farmers WHERE route_id = @id", new { id });
        
        // Delete route
        await connection.ExecuteAsync("DELETE FROM dairy.routes WHERE id = @id", new { id });
        
        return RedirectToPage();
    }
}

public class Route
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public string driver_name { get; set; } = "";
    public string vehicle_number { get; set; } = "";
    public string status { get; set; } = "";
    public decimal total_distance { get; set; }
    public int farmer_count { get; set; }
}

public class RouteSummary
{
    public int route_id { get; set; }
    public string route_name { get; set; } = "";
    public string status { get; set; } = "";
    public int collection_count { get; set; }
    public decimal total_quantity { get; set; }
}