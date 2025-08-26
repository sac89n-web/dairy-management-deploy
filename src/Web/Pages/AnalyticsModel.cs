using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class AnalyticsModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AnalyticsModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public decimal AvgDailyCollection { get; set; }
    public decimal CollectionGrowth { get; set; }
    public decimal RevenuePerLiter { get; set; }
    public decimal RevenueGrowth { get; set; }
    public int ActiveFarmers { get; set; }
    public decimal FarmerRetention { get; set; }
    public decimal AvgFatContent { get; set; }
    public decimal QualityScore { get; set; }
    
    public List<string> ChartLabels { get; set; } = new();
    public List<decimal> CollectionData { get; set; } = new();
    public List<decimal> RevenueData { get; set; } = new();
    
    public List<TopFarmer> TopFarmers { get; set; } = new();
    public List<ProductPerformance> ProductPerformance { get; set; } = new();

    public async Task OnGetAsync(int days = 30)
    {
        using var connection = GetConnection();
        
        var startDate = DateTime.Today.AddDays(-days);
        var previousStartDate = startDate.AddDays(-days);
        
        // KPI Calculations
        var currentPeriodStats = await connection.QuerySingleOrDefaultAsync(@"
            SELECT 
                COALESCE(AVG(qty_ltr), 0) as avg_daily_collection,
                COALESCE(AVG(due_amt / qty_ltr), 0) as revenue_per_liter,
                COUNT(DISTINCT farmer_id) as active_farmers,
                COALESCE(AVG(fat_pct), 0) as avg_fat_content
            FROM dairy.milk_collection 
            WHERE date >= @startDate", new { startDate });

        var previousPeriodStats = await connection.QuerySingleOrDefaultAsync(@"
            SELECT 
                COALESCE(AVG(qty_ltr), 0) as avg_daily_collection,
                COALESCE(AVG(due_amt / qty_ltr), 0) as revenue_per_liter
            FROM dairy.milk_collection 
            WHERE date >= @previousStartDate AND date < @startDate", 
            new { previousStartDate, startDate });

        AvgDailyCollection = currentPeriodStats?.avg_daily_collection ?? 0;
        RevenuePerLiter = currentPeriodStats?.revenue_per_liter ?? 0;
        ActiveFarmers = (int)(currentPeriodStats?.active_farmers ?? 0);
        AvgFatContent = currentPeriodStats?.avg_fat_content ?? 0;

        // Growth calculations
        if (previousPeriodStats?.avg_daily_collection > 0)
        {
            CollectionGrowth = ((AvgDailyCollection - previousPeriodStats.avg_daily_collection) / previousPeriodStats.avg_daily_collection) * 100;
        }
        
        if (previousPeriodStats?.revenue_per_liter > 0)
        {
            RevenueGrowth = ((RevenuePerLiter - previousPeriodStats.revenue_per_liter) / previousPeriodStats.revenue_per_liter) * 100;
        }

        // Quality score (based on fat content and consistency)
        QualityScore = Math.Min(10, (AvgFatContent / 4.5m) * 10);
        
        // Farmer retention (farmers active in both periods)
        var totalFarmers = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(DISTINCT farmer_id) FROM dairy.milk_collection WHERE date >= @previousStartDate", 
            new { previousStartDate });
        
        FarmerRetention = totalFarmers > 0 ? (ActiveFarmers * 100m / totalFarmers) : 0;

        // Chart data
        var chartData = await connection.QueryAsync(@"
            SELECT 
                DATE(date) as chart_date,
                SUM(qty_ltr) as daily_collection,
                SUM(due_amt) as daily_revenue
            FROM dairy.milk_collection 
            WHERE date >= @startDate
            GROUP BY DATE(date)
            ORDER BY DATE(date)", new { startDate });

        ChartLabels = chartData.Select(d => ((DateTime)d.chart_date).ToString("MM/dd")).ToList();
        CollectionData = chartData.Select(d => (decimal)d.daily_collection).ToList();
        RevenueData = chartData.Select(d => (decimal)d.daily_revenue).ToList();

        // Top farmers
        TopFarmers = (await connection.QueryAsync<TopFarmer>(@"
            SELECT 
                f.name as farmer_name,
                AVG(mc.qty_ltr) as avg_daily_quantity,
                AVG(mc.fat_pct) as avg_fat_percentage,
                SUM(mc.due_amt) as total_revenue
            FROM dairy.milk_collection mc
            JOIN dairy.farmer f ON mc.farmer_id = f.id
            WHERE mc.date >= @startDate
            GROUP BY f.id, f.name
            ORDER BY total_revenue DESC
            LIMIT 10", new { startDate })).ToList();

        // Product performance
        ProductPerformance = (await connection.QueryAsync<ProductPerformance>(@"
            SELECT 
                p.name as product_name,
                COALESCE(SUM(ii.quantity), 0) as units_sold,
                COALESCE(SUM(ii.total_price), 0) as revenue,
                0 as growth
            FROM dairy.products p
            LEFT JOIN dairy.invoice_items ii ON p.id = ii.product_id
            LEFT JOIN dairy.invoices i ON ii.invoice_id = i.id AND i.invoice_date >= @startDate
            GROUP BY p.id, p.name
            ORDER BY revenue DESC", new { startDate })).ToList();
    }
}

public class TopFarmer
{
    public string farmer_name { get; set; } = "";
    public decimal avg_daily_quantity { get; set; }
    public decimal avg_fat_percentage { get; set; }
    public decimal total_revenue { get; set; }
}

public class ProductPerformance
{
    public string product_name { get; set; } = "";
    public decimal units_sold { get; set; }
    public decimal revenue { get; set; }
    public decimal growth { get; set; }
}