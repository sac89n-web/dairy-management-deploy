using Dairy.Domain;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Dairy.Application
{
    public class PaymentCustomerValidator : AbstractValidator<PaymentCustomer>
    {
        public PaymentCustomerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0);
            RuleFor(x => x.CustomerId)
                .GreaterThan(0);
            RuleFor(x => x.SaleId)
                .GreaterThan(0);
            RuleFor(x => x.Date)
                .NotEmpty();
        }
    }
}
