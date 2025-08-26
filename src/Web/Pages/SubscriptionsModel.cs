using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class SubscriptionsModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SubscriptionsModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<Subscription> Subscriptions { get; set; } = new();
    public List<Farmer> Customers { get; set; } = new();
    public List<InventoryItem> Products { get; set; } = new();
    public int ActiveSubscriptions { get; set; }
    public int TodayDeliveries { get; set; }
    public int PendingDeliveries { get; set; }
    public decimal MonthlyRevenue { get; set; }

    public async Task OnGetAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        
        Subscriptions = (await connection.QueryAsync<Subscription>(@"
            SELECT s.*, c.name as customer_name, p.name as product_name, p.unit
            FROM dairy.subscriptions s
            JOIN dairy.customer c ON s.customer_id = c.id
            JOIN dairy.products p ON s.product_id = p.id
            ORDER BY s.next_delivery_date")).ToList();

        Customers = (await connection.QueryAsync<Farmer>("SELECT id, name FROM dairy.customer ORDER BY name")).ToList();
        Products = (await connection.QueryAsync<InventoryItem>("SELECT id, name FROM dairy.products ORDER BY name")).ToList();

        // Statistics
        ActiveSubscriptions = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM dairy.subscriptions WHERE status = 'Active'");
        
        var today = DateTime.Today;
        TodayDeliveries = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM dairy.subscription_deliveries WHERE DATE(delivery_date) = @today", new { today });
        
        PendingDeliveries = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM dairy.subscriptions WHERE status = 'Active' AND next_delivery_date <= @today", new { today });
        
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
        MonthlyRevenue = await connection.QuerySingleOrDefaultAsync<decimal>(@"
            SELECT COALESCE(SUM(s.quantity * p.price), 0)
            FROM dairy.subscription_deliveries sd
            JOIN dairy.subscriptions s ON sd.subscription_id = s.id
            JOIN dairy.products p ON s.product_id = p.id
            WHERE sd.delivery_date >= @firstDay AND sd.delivery_date < @nextMonth",
            new { firstDay = firstDayOfMonth, nextMonth = firstDayOfMonth.AddMonths(1) });
    }

    public async Task<IActionResult> OnPostAddSubscriptionAsync(int customerId, int productId, decimal quantity, string frequency, DateTime startDate)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var nextDelivery = frequency switch
        {
            "Daily" => startDate.AddDays(1),
            "Weekly" => startDate.AddDays(7),
            "Monthly" => startDate.AddMonths(1),
            _ => startDate.AddDays(1)
        };

        await connection.ExecuteAsync(@"
            INSERT INTO dairy.subscriptions (customer_id, product_id, quantity, frequency, start_date, next_delivery_date, status)
            VALUES (@customerId, @productId, @quantity, @frequency, @startDate, @nextDelivery, 'Active')",
            new { customerId, productId, quantity, frequency, startDate, nextDelivery });
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeliverAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        // Record delivery
        await connection.ExecuteAsync(@"
            INSERT INTO dairy.subscription_deliveries (subscription_id, delivery_date, status)
            VALUES (@id, @today, 'Delivered')",
            new { id, today = DateTime.Today });

        // Update next delivery date
        var subscription = await connection.QuerySingleAsync<Subscription>(
            "SELECT * FROM dairy.subscriptions WHERE id = @id", new { id });

        var nextDelivery = subscription.frequency switch
        {
            "Daily" => DateTime.Today.AddDays(1),
            "Weekly" => DateTime.Today.AddDays(7),
            "Monthly" => DateTime.Today.AddMonths(1),
            _ => DateTime.Today.AddDays(1)
        };

        await connection.ExecuteAsync(
            "UPDATE dairy.subscriptions SET next_delivery_date = @nextDelivery WHERE id = @id",
            new { id, nextDelivery });
        
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostPauseAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE dairy.subscriptions SET status = 'Paused' WHERE id = @id", new { id });
        
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        // Delete subscription deliveries first
        await connection.ExecuteAsync("DELETE FROM dairy.subscription_deliveries WHERE subscription_id = @id", new { id });
        
        // Delete subscription
        await connection.ExecuteAsync("DELETE FROM dairy.subscriptions WHERE id = @id", new { id });
        
        return RedirectToPage();
    }
}

public class Subscription
{
    public int id { get; set; }
    public int customer_id { get; set; }
    public string customer_name { get; set; } = "";
    public int product_id { get; set; }
    public string product_name { get; set; } = "";
    public string unit { get; set; } = "";
    public decimal quantity { get; set; }
    public string frequency { get; set; } = "";
    public DateTime start_date { get; set; }
    public DateTime next_delivery_date { get; set; }
    public string status { get; set; } = "";
}

