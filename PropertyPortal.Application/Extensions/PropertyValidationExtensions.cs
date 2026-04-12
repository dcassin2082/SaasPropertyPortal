using FluentValidation;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.Application.Extensions
{
    public static class PropertyValidationExtensions
    {
        public static void ApplyPropertyRules<T>(this AbstractValidator<T> validator) where T : Property
        {
            validator.RuleFor(x => x.Name).NotEmpty().WithMessage("Property Name is required.").MaximumLength(200);
        }
    }

}
