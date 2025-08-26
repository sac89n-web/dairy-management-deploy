using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class InventoryModel : PageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public InventoryModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<StockSummaryItem> StockSummary { get; set; } = new();
    public List<InventoryTransaction> RecentTransactions { get; set; } = new();
    public List<InventoryItem> Items { get; set; } = new();

    public async Task OnGetAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        
        StockSummary = (await connection.QueryAsync<StockSummaryItem>(@"
            SELECT i.name as ItemName, i.unit, 
                   COALESCE(SUM(CASE WHEN it.transaction_type = 'IN' THEN it.quantity ELSE -it.quantity END), 0) as CurrentStock,
                   i.min_stock as MinStock
            FROM dairy.inventory_items i
            LEFT JOIN dairy.inventory_transactions it ON i.id = it.item_id
            GROUP BY i.id, i.name, i.unit, i.min_stock
            ORDER BY i.name")).ToList();

        RecentTransactions = (await connection.QueryAsync<InventoryTransaction>(@"
            SELECT it.*, i.name as item_name, i.unit
            FROM dairy.inventory_transactions it
            JOIN dairy.inventory_items i ON it.item_id = i.id
            ORDER BY it.transaction_date DESC
            LIMIT 20")).ToList();

        Items = (await connection.QueryAsync<InventoryItem>("SELECT * FROM dairy.inventory_items ORDER BY name")).ToList();
    }

    public async Task<IActionResult> OnPostStockInAsync(int itemId, decimal quantity, string reference)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO dairy.inventory_transactions (item_id, transaction_type, quantity, reference, transaction_date)
            VALUES (@itemId, 'IN', @quantity, @reference, @date)",
            new { itemId, quantity, reference, date = DateTime.Now });
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStockOutAsync(int itemId, decimal quantity, string reference)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO dairy.inventory_transactions (item_id, transaction_type, quantity, reference, transaction_date)
            VALUES (@itemId, 'OUT', @quantity, @reference, @date)",
            new { itemId, quantity, reference, date = DateTime.Now });
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM dairy.inventory WHERE id = @id", new { id });
        return RedirectToPage();
    }
}

public class StockSummaryItem
{
    public string ItemName { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal CurrentStock { get; set; }
    public decimal MinStock { get; set; }
}

public class InventoryTransaction
{
    public int id { get; set; }
    public int item_id { get; set; }
    public string item_name { get; set; } = "";
    public string unit { get; set; } = "";
    public string transaction_type { get; set; } = "";
    public decimal quantity { get; set; }
    public string reference { get; set; } = "";
    public DateTime transaction_date { get; set; }
}

public class InventoryItem
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public string unit { get; set; } = "";
    public decimal min_stock { get; set; }
}