using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class InvoicesModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public InvoicesModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<Invoice> Invoices { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public int TodayInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal TotalRevenue { get; set; }

    public async Task OnGetAsync()
    {
        using var connection = GetConnection();
        
        Invoices = (await connection.QueryAsync<Invoice>(@"
            SELECT i.*, c.name as customer_name
            FROM dairy.invoices i
            LEFT JOIN dairy.customer c ON i.customer_id = c.id
            ORDER BY i.invoice_date DESC")).ToList();

        Customers = (await connection.QueryAsync<Customer>("SELECT id, name, contact, email, city, customer_type FROM dairy.customer WHERE is_active = true ORDER BY name")).ToList();
        Products = (await connection.QueryAsync<Product>("SELECT id, name, price FROM dairy.products WHERE is_active = true ORDER BY name")).ToList();

        var today = DateTime.Today;
        TodayInvoices = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM dairy.invoices WHERE DATE(invoice_date) = @today", new { today });

        PaidInvoices = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM dairy.invoices WHERE status = 'Paid'");

        PendingAmount = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(SUM(total_amount), 0) FROM dairy.invoices WHERE status = 'Pending'");

        TotalRevenue = await connection.QuerySingleOrDefaultAsync<decimal>(
            "SELECT COALESCE(SUM(total_amount), 0) FROM dairy.invoices WHERE status = 'Paid'");
    }

    public async Task<IActionResult> OnPostGenerateInvoiceAsync(int customerId, int[] productIds, int[] quantities, decimal[] unitPrices, string paymentMethod)
    {
        using var connection = GetConnection();
        
        try
        {
            var invoiceNumber = $"INV{DateTime.Now:yyyyMMddHHmmss}";
            var subtotal = 0m;
            
            for (int i = 0; i < productIds.Length; i++)
            {
                subtotal += quantities[i] * unitPrices[i];
            }
            
            var taxAmount = subtotal * 0.18m; // 18% GST
            var totalAmount = subtotal + taxAmount;
            
            // Create tables if they don't exist
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS dairy.invoices (
                    id SERIAL PRIMARY KEY,
                    invoice_number VARCHAR(50) UNIQUE NOT NULL,
                    customer_id INTEGER NOT NULL,
                    invoice_date DATE NOT NULL,
                    subtotal DECIMAL(10,2) NOT NULL,
                    tax_amount DECIMAL(10,2) NOT NULL,
                    total_amount DECIMAL(10,2) NOT NULL,
                    payment_method VARCHAR(20) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    paid_date DATE,
                    created_at TIMESTAMP DEFAULT NOW()
                )");
            
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS dairy.invoice_items (
                    id SERIAL PRIMARY KEY,
                    invoice_id INTEGER NOT NULL,
                    product_id INTEGER NOT NULL,
                    quantity DECIMAL(10,2) NOT NULL,
                    unit_price DECIMAL(10,2) NOT NULL,
                    total_price DECIMAL(10,2) NOT NULL
                )");
            
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS dairy.products (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    price DECIMAL(10,2) NOT NULL,
                    is_active BOOLEAN DEFAULT true
                )");
            
            // Insert default products if empty
            var productCount = await connection.QuerySingleOrDefaultAsync<int>("SELECT COUNT(*) FROM dairy.products");
            if (productCount == 0)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO dairy.products (name, price) VALUES
                    ('Full Cream Milk (1L)', 60.00),
                    ('Toned Milk (1L)', 55.00),
                    ('Skimmed Milk (1L)', 50.00),
                    ('Butter (500g)', 250.00),
                    ('Paneer (250g)', 120.00),
                    ('Curd (500g)', 40.00)");
            }
            
            var invoiceId = await connection.QuerySingleAsync<int>(@"
                INSERT INTO dairy.invoices (invoice_number, customer_id, invoice_date, subtotal, tax_amount, total_amount, payment_method, status)
                VALUES (@invoiceNumber, @customerId, @today, @subtotal, @taxAmount, @totalAmount, @paymentMethod, @status)
                RETURNING id",
                new { 
                    invoiceNumber, 
                    customerId, 
                    today = DateTime.Today, 
                    subtotal, 
                    taxAmount, 
                    totalAmount, 
                    paymentMethod,
                    status = paymentMethod == "Credit" ? "Pending" : "Paid"
                });

            // Insert invoice items
            for (int i = 0; i < productIds.Length; i++)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO dairy.invoice_items (invoice_id, product_id, quantity, unit_price, total_price)
                    VALUES (@invoiceId, @productId, @quantity, @unitPrice, @totalPrice)",
                    new { 
                        invoiceId, 
                        productId = productIds[i], 
                        quantity = quantities[i], 
                        unitPrice = unitPrices[i], 
                        totalPrice = quantities[i] * unitPrices[i] 
                    });
            }
        }
        catch (Exception ex)
        {
            // Log error and continue
            Console.WriteLine($"Invoice generation error: {ex.Message}");
        }
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkPaidAsync(int id)
    {
        using var connection = GetConnection();
        
        await connection.ExecuteAsync(@"
            UPDATE dairy.invoices 
            SET status = 'Paid', paid_date = @today 
            WHERE id = @id", new { id, today = DateTime.Today });
        
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        using var connection = GetConnection();
        
        // Delete invoice items first
        await connection.ExecuteAsync("DELETE FROM dairy.invoice_items WHERE invoice_id = @id", new { id });
        
        // Delete invoice
        await connection.ExecuteAsync("DELETE FROM dairy.invoices WHERE id = @id", new { id });
        
        return RedirectToPage();
    }
}

public class Invoice
{
    public int id { get; set; }
    public string invoice_number { get; set; } = "";
    public int customer_id { get; set; }
    public string customer_name { get; set; } = "";
    public DateTime invoice_date { get; set; }
    public decimal subtotal { get; set; }
    public decimal tax_amount { get; set; }
    public decimal total_amount { get; set; }
    public string payment_method { get; set; } = "";
    public string status { get; set; } = "";
    public DateTime? paid_date { get; set; }
}



public class Product
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public decimal price { get; set; }
}