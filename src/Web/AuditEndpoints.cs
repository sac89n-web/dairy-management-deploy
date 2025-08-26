using Dairy.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

public static class AuditEndpoints
{
    [Authorize(Roles = "Admin")]
    public static async Task<IResult> Log([FromServices] IAuditLogRepository repo, [FromBody] AuditLogRequest request)
    {
        await repo.LogAsync(request.UserId, request.Action, request.Entity, request.EntityId, request.Details);
        return Results.Ok();
    }
}

public class AuditLogRequest
{
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
}
