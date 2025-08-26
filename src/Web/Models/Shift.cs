public class Shift
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public TimeSpan? start_time { get; set; }
    public TimeSpan? end_time { get; set; }
}