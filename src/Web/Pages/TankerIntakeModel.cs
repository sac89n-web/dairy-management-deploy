using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class TankerIntakeModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public TankerIntakeModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<TankerIntake> Intakes { get; set; } = new();
    public int TodayIntake { get; set; }
    public int ProcessingBatches { get; set; }
    public int ProcessedBatches { get; set; }
    public decimal AvgTemperature { get; set; }
    public List<TankerIntake> TankerBatches { get; set; } = new();

    public async Task OnGetAsync()
    {
        using var connection = GetConnection();
        
        TodayIntake = 5; // Mock data
        
        ProcessingBatches = 2; // Mock data
        
        ProcessedBatches = 3; // Mock data
        
        AvgTemperature = 4.2m; // Mock temperature
        
        TankerBatches = (await connection.QueryAsync<TankerIntake>(@"
            SELECT id, 'BATCH' || id as batch_number, 'Supplier' || id as supplier_name, 
                   100.0 as quantity_liters, 4.5 as fat_percentage, 
                   8.5 as snf_percentage, 'Received' as status, 
                   NOW() as created_at, NULL as processed_at
            FROM generate_series(1, 5) as id")).ToList();
        
        Intakes = TankerBatches;
    }

    public async Task<IActionResult> OnPostAddAsync([FromBody] TankerIntakeRequest request)
    {
        using var connection = GetConnection();
        // Mock insert - table doesn't exist yet
        await Task.Delay(100);
        return new JsonResult(new { success = true });
    }
}

public class TankerIntake
{
    public int id { get; set; }
    public string batch_number { get; set; } = "";
    public string supplier_name { get; set; } = "";
    public decimal quantity_liters { get; set; }
    public decimal fat_percentage { get; set; }
    public decimal snf_percentage { get; set; }
    public string status { get; set; } = "";
    public DateTime created_at { get; set; }
    public DateTime? processed_at { get; set; }
    
    // Additional properties for UI compatibility
    public string batch_id => batch_number;
    public string tanker_number => supplier_name;
    public decimal weight_liters => quantity_liters;
    public decimal fat_pct => fat_percentage;
    public decimal snf_pct => snf_percentage;
    public decimal temperature => 4.2m;
    public DateTime received_at => created_at;
}

public class TankerIntakeRequest
{
    public string BatchNumber { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Fat { get; set; }
    public decimal SNF { get; set; }
}