using Dairy.Domain;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Dairy.Application
{
    public class PaymentFarmerValidator : AbstractValidator<PaymentFarmer>
    {
        public PaymentFarmerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0);
            RuleFor(x => x.FarmerId)
                .GreaterThan(0);
            RuleFor(x => x.MilkCollectionId)
                .GreaterThan(0);
            RuleFor(x => x.Date)
                .NotEmpty();
        }
    }
}
