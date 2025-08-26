using Dairy.Domain;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Dairy.Application
{
    public class MilkCollectionValidator : AbstractValidator<MilkCollection>
    {
        public MilkCollectionValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.QtyLtr)
                .GreaterThan(0).LessThanOrEqualTo(999)
                .WithMessage(localizer["Error_QuantityRange"]);
            RuleFor(x => x.FatPct)
                .InclusiveBetween(0, 15)
                .WithMessage(localizer["Error_FatPctRange"]);
            RuleFor(x => x.PricePerLtr)
                .GreaterThan(0);
            RuleFor(x => x.DueAmt)
                .GreaterThanOrEqualTo(0);
            RuleFor(x => x.FarmerId)
                .GreaterThan(0);
            RuleFor(x => x.ShiftId)
                .GreaterThan(0);
            RuleFor(x => x.Date)
                .NotEmpty();
        }
    }
}
