using Dairy.Domain;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Dairy.Application
{
    public class SaleValidator : AbstractValidator<Sale>
    {
        public SaleValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.QtyLtr)
                .GreaterThan(0).LessThanOrEqualTo(999)
                .WithMessage(localizer["Error_QuantityRange"]);
            RuleFor(x => x.PaidAmt)
                .LessThanOrEqualTo(x => x.DueAmt)
                .WithMessage(localizer["Error_PaidExceedsDue"]);
            RuleFor(x => x.UnitPrice)
                .GreaterThan(0);
            RuleFor(x => x.CustomerId)
                .GreaterThan(0);
            RuleFor(x => x.ShiftId)
                .GreaterThan(0);
            RuleFor(x => x.Date)
                .NotEmpty();
        }
    }
}
