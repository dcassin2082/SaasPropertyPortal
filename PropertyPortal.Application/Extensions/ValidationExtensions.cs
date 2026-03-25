using FluentValidation;
using PropertyPortal.Application.Validators.Common;
using PropertyPortal.Domain.Core.Interfaces;

namespace PropertyPortal.Application.Extensions
{
    public static class ValidationExtensions
    {
        public static void ApplyLocatableRules<T>(this AbstractValidator<T> validator)
            where T : ILocatable
        {
            validator.RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required").MaximumLength(200);
            validator.RuleFor(x => x.Description).MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

            // This nests the AddressValidator inside the parent validator
            validator.RuleFor(x => x.Address).SetValidator(new AddressValidator());
        }
    }

}
