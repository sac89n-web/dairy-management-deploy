using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class SubsidiesModel : PageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SubsidiesModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public decimal CowMilkSubsidy { get; set; }
    public decimal TotalApplied { get; set; }
    public decimal PendingApproval { get; set; }
    public decimal ReceivedAmount { get; set; }
    public List<SubsidyApplication> SubsidyApplications { get; set; } = new();

    public async Task OnGetAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        
        // Calculate cow milk subsidy (₹5 per liter for last month)
        CowMilkSubsidy = await connection.QuerySingleOrDefaultAsync<decimal>(@"
            SELECT COALESCE(SUM(qty_ltr * 5), 0) 
            FROM dairy.milk_collection 
            WHERE date >= DATE_TRUNC('month', CURRENT_DATE - INTERVAL '1 month')
            AND date < DATE_TRUNC('month', CURRENT_DATE)");
        
        // Mock subsidy applications data
        SubsidyApplications = new List<SubsidyApplication>
        {
            new() { 
                application_id = "SUB2024001", 
                scheme_name = "Cow Milk Subsidy", 
                period_from = DateTime.Now.AddMonths(-2), 
                period_to = DateTime.Now.AddMonths(-1),
                amount = CowMilkSubsidy,
                status = "Approved",
                applied_date = DateTime.Now.AddDays(-15)
            }
        };
        
        TotalApplied = SubsidyApplications.Sum(s => s.amount);
        PendingApproval = SubsidyApplications.Where(s => s.status == "Pending").Sum(s => s.amount);
        ReceivedAmount = SubsidyApplications.Where(s => s.status == "Approved").Sum(s => s.amount);
    }

    public async Task<IActionResult> OnPostApplySubsidyAsync(int schemeId, DateTime periodFrom, DateTime periodTo)
    {
        // Logic to apply for subsidy
        return RedirectToPage();
    }
}

public class SubsidyApplication
{
    public string application_id { get; set; } = "";
    public string scheme_name { get; set; } = "";
    public DateTime period_from { get; set; }
    public DateTime period_to { get; set; }
    public decimal amount { get; set; }
    public string status { get; set; } = "";
    public DateTime applied_date { get; set; }
}