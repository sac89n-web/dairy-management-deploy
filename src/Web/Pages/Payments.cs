using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Domain;
using System.Collections.Generic;

public class PaymentsModel : PageModel
{
    public List<PaymentCustomer> CustomerPayments { get; set; } = new();
    public List<PaymentFarmer> FarmerPayments { get; set; } = new();

    public void OnGet()
    {
        // TODO: Query from DB
        CustomerPayments.Add(new PaymentCustomer { Id = 1, CustomerId = 1, SaleId = 1, Amount = 100, Date = "2025-08-18", InvoiceNo = "INV001", PdfPath = "invoice.pdf" });
        FarmerPayments.Add(new PaymentFarmer { Id = 1, FarmerId = 1, MilkCollectionId = 1, Amount = 100, Date = "2025-08-18", InvoiceNo = "INV002", PdfPath = "invoice.pdf" });
    }
}
