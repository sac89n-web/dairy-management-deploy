namespace Dairy.Domain
{
    public class PaymentFarmer
    {
        public int Id { get; set; }
        public int FarmerId { get; set; }
        public int MilkCollectionId { get; set; }
        public decimal Amount { get; set; }
        public string Date { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string PdfPath { get; set; } = string.Empty;
    }
}
