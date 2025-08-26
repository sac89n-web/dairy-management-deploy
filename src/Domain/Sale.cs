namespace Dairy.Domain
{
    public class Sale
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int ShiftId { get; set; }
        public DateTime Date { get; set; }
        public decimal QtyLtr { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmt { get; set; }
        public decimal DueAmt { get; set; }
        public int CreatedBy { get; set; }
    }
}
