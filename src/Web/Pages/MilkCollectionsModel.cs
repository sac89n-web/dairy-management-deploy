using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class MilkCollectionsModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public MilkCollectionsModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<MilkCollection> Collections { get; set; } = new();
    public List<Farmer> Farmers { get; set; } = new();
    public List<Shift> Shifts { get; set; } = new();
    public ShiftSummary MorningSummary { get; set; } = new();
    public ShiftSummary EveningSummary { get; set; } = new();

    public async Task OnGetAsync()
    {
        using var connection = GetConnection();
        Collections = (await connection.QueryAsync<MilkCollection>(
            "SELECT mc.id, f.name as farmer_name, s.name as shift_name, mc.qty_ltr as quantity, mc.fat_pct as fat_percentage, mc.price_per_ltr as rate_per_liter, mc.due_amt as total_amount, mc.date as collection_date, mc.notes, CASE WHEN pf.id IS NOT NULL THEN 'Paid' ELSE 'Pending' END as payment_status FROM dairy.milk_collection mc JOIN dairy.farmer f ON mc.farmer_id = f.id JOIN dairy.shift s ON mc.shift_id = s.id LEFT JOIN dairy.payment_farmer pf ON mc.id = pf.milk_collection_id ORDER BY mc.date DESC, s.id")).ToList();
        
        Farmers = (await connection.QueryAsync<Farmer>("SELECT f.id, f.name, f.code, f.contact, f.bank_id, f.branch_id FROM dairy.farmer f ORDER BY f.name")).ToList();
        Shifts = (await connection.QueryAsync<Shift>("SELECT id, name, start_time, end_time FROM dairy.shift ORDER BY id")).ToList();
        
        // Get shift summaries for today
        var today = DateTime.Today;
        MorningSummary = await connection.QuerySingleOrDefaultAsync<ShiftSummary>(
            "SELECT COALESCE(SUM(qty_ltr), 0) as TotalQuantity, COALESCE(SUM(due_amt), 0) as TotalAmount FROM dairy.milk_collection WHERE DATE(date) = @today AND shift_id = 1", 
            new { today }) ?? new ShiftSummary();
        EveningSummary = await connection.QuerySingleOrDefaultAsync<ShiftSummary>(
            "SELECT COALESCE(SUM(qty_ltr), 0) as TotalQuantity, COALESCE(SUM(due_amt), 0) as TotalAmount FROM dairy.milk_collection WHERE DATE(date) = @today AND shift_id = 2", 
            new { today }) ?? new ShiftSummary();
    }

    public async Task<IActionResult> OnPostAddAsync(int farmerId, int shiftId, decimal quantity, decimal fatPercentage, decimal ratePerLiter)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(
            "INSERT INTO dairy.milk_collection (farmer_id, shift_id, date, qty_ltr, fat_pct, price_per_ltr, due_amt, created_by) VALUES (@farmerId, @shiftId, @date, @quantity, @fatPercentage, @ratePerLiter, @totalAmount, 1)",
            new { farmerId, shiftId, quantity, fatPercentage, ratePerLiter, totalAmount = quantity * ratePerLiter, date = DateTime.Now.Date });
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        using var connection = GetConnection();
        
        // Check if payment exists
        var hasPayment = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM dairy.payment_farmer WHERE milk_collection_id = @id", new { id });
        
        if (hasPayment > 0)
        {
            TempData["Error"] = "Cannot delete collection with existing payment";
            return RedirectToPage();
        }
        
        await connection.ExecuteAsync("DELETE FROM dairy.milk_collection WHERE id = @id", new { id });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetGetCollectionAsync(int id)
    {
        using var connection = GetConnection();
        var collection = await connection.QuerySingleOrDefaultAsync(
            "SELECT id, farmer_id, shift_id, qty_ltr as quantity, fat_pct as fat_percentage, price_per_ltr as rate_per_liter FROM dairy.milk_collection WHERE id = @id", 
            new { id });
        return new JsonResult(collection);
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, int farmerId, int shiftId, decimal quantity, decimal fatPercentage, decimal ratePerLiter)
    {
        using var connection = GetConnection();
        
        // Check if payment exists
        var hasPayment = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM dairy.payment_farmer WHERE milk_collection_id = @id", new { id });
        
        if (hasPayment > 0)
        {
            TempData["Error"] = "Cannot modify collection with existing payment";
            return RedirectToPage();
        }
        
        await connection.ExecuteAsync(@"
            UPDATE dairy.milk_collection SET 
                farmer_id = @farmerId, shift_id = @shiftId, qty_ltr = @quantity, 
                fat_pct = @fatPercentage, price_per_ltr = @ratePerLiter, 
                due_amt = @totalAmount
            WHERE id = @id",
            new { id, farmerId, shiftId, quantity, fatPercentage, ratePerLiter, totalAmount = quantity * ratePerLiter });
        return RedirectToPage();
    }
}

public class MilkCollection
{
    public int id { get; set; }
    public string farmer_name { get; set; } = "";
    public string shift_name { get; set; } = "";
    public decimal quantity { get; set; }
    public decimal fat_percentage { get; set; }
    public decimal rate_per_liter { get; set; }
    public decimal total_amount { get; set; }
    public DateTime collection_date { get; set; }
    public string? notes { get; set; }
    public string payment_status { get; set; } = "Pending";
    public DateTime? payment_date { get; set; }
}



public class ShiftSummary
{
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
}

