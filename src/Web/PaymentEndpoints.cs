using Dairy.Infrastructure;
using Dairy.Domain;
using Dairy.Application;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

public static class PaymentEndpoints
{
    public static async Task<IResult> ListCustomerPayments([FromServices] IPaymentCustomerRepository repo, [FromQuery] int? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await repo.ListAsync(customerId, page, pageSize);
        return Results.Ok(result);
    }

    [Authorize(Roles = "Admin,CollectionBoy")]
    public static async Task<IResult> AddCustomerPayment([FromServices] IPaymentCustomerRepository repo, [FromServices] IValidator<PaymentCustomer> validator, [FromBody] PaymentCustomer entity)
    {
        var validation = await validator.ValidateAsync(entity);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(validation.Errors.Select(e => e.ErrorMessage));
        var id = await repo.AddAsync(entity);
        return Results.Created($"/api/payments/customers/{id}", new { id });
    }

    public static async Task<IResult> ListFarmerPayments([FromServices] IPaymentFarmerRepository repo, [FromQuery] int? farmerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await repo.ListAsync(farmerId, page, pageSize);
        return Results.Ok(result);
    }

    [Authorize(Roles = "Admin")]
    public static async Task<IResult> AddFarmerPayment([FromServices] IPaymentFarmerRepository repo, [FromServices] IValidator<PaymentFarmer> validator, [FromBody] PaymentFarmer entity)
    {
        var validation = await validator.ValidateAsync(entity);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(validation.Errors.Select(e => e.ErrorMessage));
        var id = await repo.AddAsync(entity);
        return Results.Created($"/api/payments/farmers/{id}", new { id });
    }
}
