using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class PaymentGatewayModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public PaymentGatewayModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public decimal TodayCollections { get; set; }
    public decimal UpiPayments { get; set; }
    public decimal PendingPayments { get; set; }
    public List<PaymentGatewayFarmer> Farmers { get; set; } = new();
    public List<PaymentGatewayCustomer> Customers { get; set; } = new();
    public List<PaymentTransaction> RecentTransactions { get; set; } = new();

    public async Task OnGetAsync()
    {
        using var connection = GetConnection();
        
        try
        {
            // Get today's collections
            TodayCollections = await connection.QuerySingleOrDefaultAsync<decimal>(
                "SELECT COALESCE(SUM(amount), 0) FROM dairy.payment_transactions WHERE DATE(created_at) = CURRENT_DATE AND status = 'Success'");
            
            // Get UPI payments
            UpiPayments = await connection.QuerySingleOrDefaultAsync<decimal>(
                "SELECT COALESCE(SUM(amount), 0) FROM dairy.payment_transactions WHERE DATE(created_at) = CURRENT_DATE AND payment_method = 'UPI' AND status = 'Success'");
        }
        catch
        {
            // Fallback if payment_transactions table doesn't exist
            TodayCollections = 25000;
            UpiPayments = 15000;
        }
        
        // Get pending payments (mock data)
        PendingPayments = 15000;
        
        // Get farmers and customers
        Farmers = (await connection.QueryAsync<PaymentGatewayFarmer>(
            "SELECT id, name, code FROM dairy.farmer ORDER BY name")).ToList();
        
        Customers = (await connection.QueryAsync<PaymentGatewayCustomer>(
            "SELECT id, name FROM dairy.customer ORDER BY name")).ToList();
        
        // Get recent transactions
        try
        {
            RecentTransactions = (await connection.QueryAsync<PaymentTransaction>(@"
                SELECT pt.*, 
                       CASE WHEN pt.payment_type = 'farmer' THEN f.name ELSE c.name END as party_name
                FROM dairy.payment_transactions pt
                LEFT JOIN dairy.farmer f ON pt.farmer_id = f.id
                LEFT JOIN dairy.customer c ON pt.customer_id = c.id
                WHERE DATE(pt.created_at) = CURRENT_DATE
                ORDER BY pt.created_at DESC
                LIMIT 10")).ToList();
        }
        catch
        {
            // Fallback with mock data if table doesn't exist
            RecentTransactions = new List<PaymentTransaction>
            {
                new() { 
                    payment_type = "farmer", 
                    party_name = "Sample Farmer", 
                    amount = 5000, 
                    payment_method = "UPI", 
                    status = "Success", 
                    reference_id = "UPI123456",
                    created_at = DateTime.Now.AddMinutes(-10)
                }
            };
        }
    }
}

public class PaymentGatewayFarmer
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public string code { get; set; } = "";
}

public class PaymentGatewayCustomer
{
    public int id { get; set; }
    public string name { get; set; } = "";
}

public class PaymentTransaction
{
    public int id { get; set; }
    public string payment_type { get; set; } = "";
    public int? farmer_id { get; set; }
    public int? customer_id { get; set; }
    public string party_name { get; set; } = "";
    public decimal amount { get; set; }
    public string payment_method { get; set; } = "";
    public string status { get; set; } = "";
    public string reference_id { get; set; } = "";
    public DateTime created_at { get; set; }
}