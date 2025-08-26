using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class ViewInvoiceModel : PageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ViewInvoiceModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public InvoiceDetail? Invoice { get; set; }
    public List<InvoiceItemDetail> InvoiceItems { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        Invoice = await connection.QuerySingleOrDefaultAsync<InvoiceDetail>(@"
            SELECT i.*, c.name as customer_name, c.contact as customer_contact
            FROM dairy.invoices i
            LEFT JOIN dairy.customer c ON i.customer_id = c.id
            WHERE i.id = @id", new { id });

        if (Invoice == null)
        {
            return NotFound();
        }

        InvoiceItems = (await connection.QueryAsync<InvoiceItemDetail>(@"
            SELECT ii.*, p.name as product_name
            FROM dairy.invoice_items ii
            LEFT JOIN dairy.products p ON ii.product_id = p.id
            WHERE ii.invoice_id = @id", new { id })).ToList();

        return Page();
    }
}

public class InvoiceDetail
{
    public int id { get; set; }
    public string invoice_number { get; set; } = "";
    public int customer_id { get; set; }
    public string customer_name { get; set; } = "";
    public string customer_contact { get; set; } = "";
    public DateTime invoice_date { get; set; }
    public decimal subtotal { get; set; }
    public decimal tax_amount { get; set; }
    public decimal total_amount { get; set; }
    public string payment_method { get; set; } = "";
    public string status { get; set; } = "";
    public DateTime? paid_date { get; set; }
}

public class InvoiceItemDetail
{
    public int id { get; set; }
    public int invoice_id { get; set; }
    public int product_id { get; set; }
    public string product_name { get; set; } = "";
    public int quantity { get; set; }
    public decimal unit_price { get; set; }
    public decimal total_price { get; set; }
}