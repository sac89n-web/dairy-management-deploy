using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;
using Npgsql;

public class DashboardModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public DashboardModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public decimal TodayCollection { get; set; }
    public decimal TodaySales { get; set; }
    public decimal TodayRevenue { get; set; }
    public int ActiveFarmers { get; set; }
    public int LowStockItems { get; set; }
    public int ActiveSubscriptions { get; set; }
    public List<RecentCollection> RecentCollections { get; set; } = new();
    public List<RecentSale> RecentSales { get; set; } = new();
    public List<DashboardTopFarmer> TopFarmers { get; set; } = new();
    public List<MonthlyTrend> MonthlyTrends { get; set; } = new();
    public decimal WeeklyGrowth { get; set; }
    public decimal MonthlyGrowth { get; set; }
    public int PendingPayments { get; set; }
    public decimal AvgFatContent { get; set; }
    public decimal QualityScore { get; set; }
    public decimal TotalOutstanding { get; set; }

    public async Task OnGetAsync()
    {
        // Use session connection string if available
        var sessionConnectionString = HttpContext.Session.GetString("ConnectionString");
        var connectionString = !string.IsNullOrEmpty(sessionConnectionString) 
            ? sessionConnectionString 
            : "Host=localhost;Database=postgres;Username=admin;Password=admin123;SearchPath=dairy";
            
        using var connection = new Npgsql.NpgsqlConnection(connectionString);
        
        // Today's metrics
        var today = DateTime.Today;
        TodayCollection = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(SUM(qty_ltr), 0) FROM dairy.milk_collection WHERE DATE(date) = @today", 
            new { today });
            
        TodaySales = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(SUM(qty_ltr), 0) FROM dairy.sales WHERE DATE(date) = @today", 
            new { today });
            
        TodayRevenue = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(SUM(paid_amt), 0) FROM dairy.sales WHERE DATE(date) = @today", 
            new { today });
            
        ActiveFarmers = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(DISTINCT farmer_id) FROM dairy.milk_collection WHERE DATE(date) >= @weekAgo", 
            new { weekAgo = today.AddDays(-7) });
            
        // Get inventory alerts (placeholder)
        LowStockItems = 0;
            
        // Get active subscriptions (placeholder)
        ActiveSubscriptions = 0;

        // Recent collections
        RecentCollections = (await connection.QueryAsync<RecentCollection>(
            "SELECT f.name as farmer_name, mc.qty_ltr as quantity, mc.fat_pct as fat_percentage, mc.due_amt as total_amount FROM dairy.milk_collection mc JOIN dairy.farmer f ON mc.farmer_id = f.id ORDER BY mc.date DESC LIMIT 5")).ToList();

        // Recent sales
        RecentSales = (await connection.QueryAsync<RecentSale>(
            "SELECT c.name as customer_name, s.qty_ltr as quantity, s.unit_price as rate_per_liter, s.paid_amt as total_amount FROM dairy.sales s JOIN dairy.customer c ON s.customer_id = c.id ORDER BY s.date DESC LIMIT 5")).ToList();

        // Top farmers by collection
        TopFarmers = (await connection.QueryAsync<DashboardTopFarmer>(@"
            SELECT f.name, f.code, SUM(mc.qty_ltr) as total_collection, AVG(mc.fat_pct) as avg_fat
            FROM dairy.milk_collection mc
            JOIN dairy.farmer f ON mc.farmer_id = f.id
            WHERE mc.date >= @monthStart
            GROUP BY f.id, f.name, f.code
            ORDER BY total_collection DESC
            LIMIT 5", new { monthStart = new DateTime(today.Year, today.Month, 1) })).ToList();

        // Monthly trends
        MonthlyTrends = (await connection.QueryAsync<MonthlyTrend>(@"
            SELECT DATE_TRUNC('month', date) as month, SUM(qty_ltr) as collection, SUM(due_amt) as revenue
            FROM dairy.milk_collection
            WHERE date >= @sixMonthsAgo
            GROUP BY DATE_TRUNC('month', date)
            ORDER BY month", new { sixMonthsAgo = today.AddMonths(-6) })).ToList();

        // Growth calculations
        var lastWeekCollection = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(SUM(qty_ltr), 0) FROM dairy.milk_collection WHERE date >= @lastWeekStart AND date < @thisWeekStart",
            new { lastWeekStart = today.AddDays(-14), thisWeekStart = today.AddDays(-7) });
        var thisWeekCollection = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(SUM(qty_ltr), 0) FROM dairy.milk_collection WHERE date >= @thisWeekStart",
            new { thisWeekStart = today.AddDays(-7) });
        WeeklyGrowth = lastWeekCollection > 0 ? ((thisWeekCollection - lastWeekCollection) / lastWeekCollection) * 100 : 0;

        // Additional metrics
        PendingPayments = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM dairy.milk_collection mc LEFT JOIN dairy.payment_farmer pf ON mc.id = pf.milk_collection_id WHERE pf.id IS NULL");

        AvgFatContent = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(AVG(fat_pct), 0) FROM dairy.milk_collection WHERE date >= @monthStart",
            new { monthStart = new DateTime(today.Year, today.Month, 1) });

        QualityScore = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(AVG(fat_pct), 0) FROM dairy.milk_collection WHERE date >= @monthStart",
            new { monthStart = new DateTime(today.Year, today.Month, 1) });

        TotalOutstanding = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(SUM(mc.due_amt), 0) FROM dairy.milk_collection mc LEFT JOIN dairy.payment_farmer pf ON mc.id = pf.milk_collection_id WHERE pf.id IS NULL");
    }
}

public class RecentCollection
{
    public string farmer_name { get; set; } = "";
    public decimal quantity { get; set; }
    public decimal fat_percentage { get; set; }
    public decimal total_amount { get; set; }
}

public class RecentSale
{
    public string customer_name { get; set; } = "";
    public decimal quantity { get; set; }
    public decimal rate_per_liter { get; set; }
    public decimal total_amount { get; set; }
}

public class DashboardTopFarmer
{
    public string name { get; set; } = "";
    public string code { get; set; } = "";
    public decimal total_collection { get; set; }
    public decimal avg_fat { get; set; }
}

public class MonthlyTrend
{
    public DateTime month { get; set; }
    public decimal collection { get; set; }
    public decimal revenue { get; set; }
}

