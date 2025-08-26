using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class ColdChainModel : PageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ColdChainModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<TemperatureSensor> TemperatureSensors { get; set; } = new();
    public List<string> ChartLabels { get; set; } = new();
    public List<decimal> TemperatureData { get; set; } = new();

    public async Task OnGetAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        
        // Mock data for temperature sensors
        TemperatureSensors = new List<TemperatureSensor>
        {
            new() { id = 1, location = "Storage Tank 1", temperature = 4.2m, last_reading = DateTime.Now.AddMinutes(-2) },
            new() { id = 2, location = "Storage Tank 2", temperature = 3.8m, last_reading = DateTime.Now.AddMinutes(-1) },
            new() { id = 3, location = "Delivery Vehicle", temperature = 6.1m, last_reading = DateTime.Now.AddMinutes(-5) }
        };
        
        // Generate mock temperature history
        var now = DateTime.Now;
        for (int i = 23; i >= 0; i--)
        {
            ChartLabels.Add(now.AddHours(-i).ToString("HH:mm"));
            TemperatureData.Add(4.0m + (decimal)(new Random().NextDouble() * 2 - 1)); // 3-5°C range
        }
    }
}

public class TemperatureSensor
{
    public int id { get; set; }
    public string location { get; set; } = "";
    public decimal temperature { get; set; }
    public DateTime last_reading { get; set; }
}