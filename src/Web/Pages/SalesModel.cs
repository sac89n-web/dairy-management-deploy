using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class SalesModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SalesModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<Sale> Sales { get; set; } = new();

    public async Task OnGetAsync()
    {
        using var connection = GetConnection();
        Sales = (await connection.QueryAsync<Sale>(
            "SELECT s.id, c.name as customer_name, sh.name as shift_name, s.qty_ltr as quantity, s.unit_price as rate_per_liter, s.discount, s.paid_amt as total_amount, s.due_amt, s.date as sale_date FROM dairy.sales s JOIN dairy.customer c ON s.customer_id = c.id JOIN dairy.shift sh ON s.shift_id = sh.id ORDER BY s.date DESC")).ToList();
    }

    public async Task<IActionResult> OnPostAddAsync(string customerName, decimal quantity, decimal ratePerLiter)
    {
        using var connection = GetConnection();
        var customerId = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT id FROM dairy.customer WHERE name = @customerName LIMIT 1", new { customerName });
        if (customerId == 0) {
            customerId = await connection.QuerySingleAsync<int>(
                "INSERT INTO dairy.customer (name, contact, branch_id) VALUES (@customerName, '0000000000', 1) RETURNING id",
                new { customerName });
        }
        await connection.ExecuteAsync(
            "INSERT INTO dairy.sales (customer_id, shift_id, date, qty_ltr, unit_price, discount, paid_amt, due_amt, created_by) VALUES (@customerId, 1, @date, @quantity, @ratePerLiter, 0, @totalAmount, 0, 1)",
            new { customerId, quantity, ratePerLiter, totalAmount = quantity * ratePerLiter, date = DateTime.Now.Date });
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync("DELETE FROM dairy.sales WHERE id = @id", new { id });
        return RedirectToPage();
    }
}

public class Sale
{
    public int id { get; set; }
    public string customer_name { get; set; } = "";
    public string shift_name { get; set; } = "";
    public decimal quantity { get; set; }
    public decimal rate_per_liter { get; set; }
    public decimal discount { get; set; }
    public decimal total_amount { get; set; }
    public decimal due_amt { get; set; }
    public DateTime sale_date { get; set; }
}