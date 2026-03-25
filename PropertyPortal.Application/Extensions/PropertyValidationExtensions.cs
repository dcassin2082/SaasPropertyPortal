using FluentValidation;
using PropertyPortal.Application.Validators.Common;
using PropertyPortal.Domain.Core.Interfaces;

namespace PropertyPortal.Application.Extensions
{
    public static class PropertyValidationExtensions
    {
        public static void ApplyPropertyRules<T>(this AbstractValidator<T> validator)
            where T : ILocatable // Create a simple interface for Name/Description/Address
        {
            validator.RuleFor(x => (x as ILocatable).Name)
                .NotEmpty().WithMessage("Property Name is required.")
                .MaximumLength(200);

            validator.RuleFor(x => (x as ILocatable).Description)
                .MaximumLength(500);

            validator.RuleFor(x => (x as ILocatable).Address)
                .SetValidator(new AddressValidator());
        }
    }

}
