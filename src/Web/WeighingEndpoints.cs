using Microsoft.AspNetCore.Mvc;
using Dairy.Infrastructure;
using Dapper;

public static class WeighingEndpoints
{
    public static async Task<IResult> Connect([FromServices] IWeighingMachineService weighingService, [FromBody] ConnectRequest request)
    {
        var success = await weighingService.ConnectAsync(request.Port, request.BaudRate);
        return success ? Results.Ok() : Results.BadRequest("Failed to connect");
    }

    public static async Task<IResult> Disconnect([FromServices] IWeighingMachineService weighingService)
    {
        weighingService.Disconnect();
        return Results.Ok();
    }

    public static async Task<IResult> GetCurrentWeight([FromServices] IWeighingMachineService weighingService)
    {
        var weight = await weighingService.GetCurrentWeightAsync();
        return Results.Ok(new { weight });
    }

    public static async Task<IResult> QuickAddCollection(
        [FromServices] SqlConnectionFactory connectionFactory,
        [FromBody] QuickCollectionRequest request)
    {
        using var connection = connectionFactory.CreateConnection();
        
        var amount = request.Quantity * request.Rate;
        var snf = 8.5m + (request.FatPct * 0.25m); // Auto-calculate SNF
        
        var id = await connection.QuerySingleAsync<int>(@"
            INSERT INTO dairy.milk_collection (farmer_id, shift_id, branch_id, date, qty_ltr, fat_pct, snf_pct, rate, due_amt, created_by, created_at)
            VALUES (@FarmerId, @ShiftId, 1, CURRENT_DATE, @Quantity, @FatPct, @Snf, @Rate, @Amount, 'WeighingMachine', NOW())
            RETURNING id",
            new
            {
                request.FarmerId,
                request.ShiftId,
                request.Quantity,
                request.FatPct,
                Snf = snf,
                request.Rate,
                Amount = amount
            });

        return Results.Ok(new { id });
    }
}

public class ConnectRequest
{
    public string Port { get; set; } = "";
    public int BaudRate { get; set; } = 9600;
}

public class QuickCollectionRequest
{
    public int FarmerId { get; set; }
    public int ShiftId { get; set; }
    public decimal Quantity { get; set; }
    public decimal FatPct { get; set; }
    public decimal Rate { get; set; }
}