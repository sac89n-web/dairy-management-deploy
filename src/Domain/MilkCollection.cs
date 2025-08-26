namespace Dairy.Domain
{
    public class MilkCollection
    {
        public int Id { get; set; }
        public int FarmerId { get; set; }
        public int ShiftId { get; set; }
        public DateTime Date { get; set; }
        public decimal QtyLtr { get; set; }
        public decimal FatPct { get; set; }
        public decimal PricePerLtr { get; set; }
        public decimal DueAmt { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
    }
}
