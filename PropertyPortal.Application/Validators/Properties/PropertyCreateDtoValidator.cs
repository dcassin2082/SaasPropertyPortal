using FluentValidation;
using PropertyPortal.Application.DTOs.Properties;
using PropertyPortal.Application.Extensions;

namespace PropertyPortal.Application.Validators.Properties
{
    public class PropertyCreateDtoValidator : AbstractValidator<PropertyCreateDto>
    {
        public PropertyCreateDtoValidator()
        {
            this.ApplyLocatableRules();
            //RuleFor(x => x.Name)
            //    .NotEmpty().WithMessage("Property Name is required.")
            //    .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

            //RuleFor(x => x.Description)
            //    .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

            //// Validating the Complex Type fields
            //RuleFor(x => x.Address.Street)
            //    .NotEmpty().WithMessage("Street address is required.")
            //    .MaximumLength(200);

            //RuleFor(x => x.Address.City)
            //    .NotEmpty().WithMessage("City is required.");

            //RuleFor(x => x.Address.State)
            //    .NotEmpty().WithMessage("State is required.");

            //RuleFor(x => x.Address.ZipCode)
            //    .NotEmpty().WithMessage("Zip Code is required.")
            //    .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Invalid Zip Code format.");
        }
    }

}
