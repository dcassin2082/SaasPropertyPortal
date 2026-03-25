using FluentValidation;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.Application.Validators.Common
{
    public class AddressValidator : AbstractValidator<Address>
    {
        public AddressValidator()
        {
            RuleFor(x => x.Street)
                .NotEmpty().WithMessage("Street address is required.")
                .MaximumLength(200);

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required.");

            RuleFor(x => x.State)
                .NotEmpty().WithMessage("State is required.");

            RuleFor(x => x.ZipCode)
                .NotEmpty().WithMessage("Zip Code is required.")
                .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Invalid Zip Code format.");
        }
    }

}
