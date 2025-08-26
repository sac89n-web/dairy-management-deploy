namespace Dairy.Reports
{
    public class ReportRow
    {
        public string Type { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public int ShiftId { get; set; }
        public decimal Amount { get; set; }
    }
}
