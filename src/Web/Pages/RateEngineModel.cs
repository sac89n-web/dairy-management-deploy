using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class RateEngineModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public RateEngineModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<RateSlab> RateSlabs { get; set; } = new();

    public async Task OnGetAsync()
    {
        using var connection = GetConnection();
        
        // Create rate_slabs table if not exists
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS dairy.rate_slabs (
                id SERIAL PRIMARY KEY,
                fat_min DECIMAL(5,2) NOT NULL,
                fat_max DECIMAL(5,2) NOT NULL,
                snf_min DECIMAL(5,2) NOT NULL,
                snf_max DECIMAL(5,2) NOT NULL,
                base_rate DECIMAL(8,2) NOT NULL,
                incentive DECIMAL(8,2) DEFAULT 0,
                effective_from DATE NOT NULL,
                is_active BOOLEAN DEFAULT true,
                created_at TIMESTAMP DEFAULT NOW()
            )");

        RateSlabs = (await connection.QueryAsync<RateSlab>(
            "SELECT * FROM dairy.rate_slabs ORDER BY effective_from DESC, fat_min")).ToList();

        // Insert default slabs if empty
        if (!RateSlabs.Any())
        {
            await connection.ExecuteAsync(@"
                INSERT INTO dairy.rate_slabs (fat_min, fat_max, snf_min, snf_max, base_rate, incentive, effective_from) VALUES
                (3.0, 3.5, 8.0, 8.5, 40.00, 0.00, CURRENT_DATE),
                (3.5, 4.0, 8.0, 8.5, 42.00, 1.00, CURRENT_DATE),
                (4.0, 4.5, 8.5, 9.0, 45.00, 2.00, CURRENT_DATE),
                (4.5, 5.0, 9.0, 9.5, 48.00, 3.00, CURRENT_DATE),
                (5.0, 6.0, 9.5, 10.0, 52.00, 5.00, CURRENT_DATE)");
            
            RateSlabs = (await connection.QueryAsync<RateSlab>(
                "SELECT * FROM dairy.rate_slabs ORDER BY effective_from DESC, fat_min")).ToList();
        }
    }

    public async Task<IActionResult> OnPostAddSlabAsync(decimal fatMin, decimal fatMax, decimal snfMin, decimal snfMax, decimal baseRate, decimal incentive, DateTime effectiveFrom)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO dairy.rate_slabs (fat_min, fat_max, snf_min, snf_max, base_rate, incentive, effective_from)
            VALUES (@fatMin, @fatMax, @snfMin, @snfMax, @baseRate, @incentive, @effectiveFrom)",
            new { fatMin, fatMax, snfMin, snfMax, baseRate, incentive, effectiveFrom });
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCalculateRateAsync([FromBody] RateCalculationRequest request)
    {
        using var connection = GetConnection();
        
        var slab = await connection.QuerySingleOrDefaultAsync<RateSlab>(@"
            SELECT * FROM dairy.rate_slabs 
            WHERE @fat >= fat_min AND @fat <= fat_max 
              AND @snf >= snf_min AND @snf <= snf_max 
              AND is_active = true 
              AND effective_from <= CURRENT_DATE
            ORDER BY effective_from DESC 
            LIMIT 1", new { fat = request.Fat, snf = request.Snf });

        if (slab == null)
        {
            // Default rate if no slab matches
            slab = new RateSlab { base_rate = 35.00m, incentive = 0 };
        }

        var rate = slab.base_rate + slab.incentive;
        var totalAmount = rate * request.Quantity;

        return new JsonResult(new { rate, totalAmount });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync("DELETE FROM dairy.rate_slabs WHERE id = @id", new { id });
        return RedirectToPage();
    }
}

public class RateSlab
{
    public int id { get; set; }
    public decimal fat_min { get; set; }
    public decimal fat_max { get; set; }
    public decimal snf_min { get; set; }
    public decimal snf_max { get; set; }
    public decimal base_rate { get; set; }
    public decimal incentive { get; set; }
    public DateTime effective_from { get; set; }
    public bool is_active { get; set; }
}

public class RateCalculationRequest
{
    public decimal Fat { get; set; }
    public decimal Snf { get; set; }
    public decimal Quantity { get; set; }
}