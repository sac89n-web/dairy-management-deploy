namespace Dairy.Domain
{
    public class PaymentCustomer
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int SaleId { get; set; }
        public decimal Amount { get; set; }
        public string Date { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string PdfPath { get; set; } = string.Empty;
    }
}
