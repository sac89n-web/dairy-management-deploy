using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class FarmerLoansModel : PageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public FarmerLoansModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<FarmerLoan> Loans { get; set; } = new();
    public List<Farmer> Farmers { get; set; } = new();
    public decimal TotalOutstanding { get; set; }
    public decimal MonthlyDisbursed { get; set; }
    public decimal MonthlyRecovered { get; set; }
    public int OverdueCount { get; set; }

    public async Task OnGetAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        
        Loans = (await connection.QueryAsync<FarmerLoan>(@"
            SELECT fl.*, f.name as farmer_name
            FROM dairy.farmer_loans fl
            JOIN dairy.farmer f ON fl.farmer_id = f.id
            ORDER BY fl.created_at DESC")).ToList();

        Farmers = (await connection.QueryAsync<Farmer>("SELECT id, name, code FROM dairy.farmer ORDER BY name")).ToList();

        TotalOutstanding = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(SUM(outstanding_amount), 0) FROM dairy.farmer_loans WHERE status = 'Active'");

        var thisMonth = DateTime.Today.AddDays(-DateTime.Today.Day + 1);
        MonthlyDisbursed = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(SUM(amount), 0) FROM dairy.farmer_loans WHERE DATE(created_at) >= @thisMonth", 
            new { thisMonth });

        MonthlyRecovered = await connection.QuerySingleOrDefaultAsync<decimal>(@"
            SELECT COALESCE(SUM(amount), 0) FROM dairy.loan_payments 
            WHERE DATE(payment_date) >= @thisMonth", new { thisMonth });

        OverdueCount = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM dairy.farmer_loans WHERE status = 'Active' AND due_date < @today", 
            new { today = DateTime.Today });
    }

    public async Task<IActionResult> OnPostAddLoanAsync(int farmerId, string loanType, decimal amount, DateTime dueDate, decimal interestRate)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        await connection.ExecuteAsync(@"
            INSERT INTO dairy.farmer_loans (farmer_id, loan_type, amount, outstanding_amount, due_date, interest_rate, status)
            VALUES (@farmerId, @loanType, @amount, @amount, @dueDate, @interestRate, 'Active')",
            new { farmerId, loanType, amount, dueDate, interestRate });
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRecordPaymentAsync(int id, decimal amount)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        await connection.ExecuteAsync(@"
            INSERT INTO dairy.loan_payments (loan_id, amount, payment_date)
            VALUES (@id, @amount, @today);
            
            UPDATE dairy.farmer_loans 
            SET outstanding_amount = outstanding_amount - @amount,
                status = CASE WHEN outstanding_amount - @amount <= 0 THEN 'Paid' ELSE status END
            WHERE id = @id", new { id, amount, today = DateTime.Today });
        
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        // Delete loan payments first
        await connection.ExecuteAsync("DELETE FROM dairy.loan_payments WHERE loan_id = @id", new { id });
        
        // Delete loan
        await connection.ExecuteAsync("DELETE FROM dairy.farmer_loans WHERE id = @id", new { id });
        
        return RedirectToPage();
    }
}

public class FarmerLoan
{
    public int id { get; set; }
    public int farmer_id { get; set; }
    public string farmer_name { get; set; } = "";
    public string loan_type { get; set; } = "";
    public decimal amount { get; set; }
    public decimal outstanding_amount { get; set; }
    public DateTime due_date { get; set; }
    public decimal interest_rate { get; set; }
    public string status { get; set; } = "";
    public DateTime created_at { get; set; }
}