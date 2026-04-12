using FluentValidation;
using PropertyPortal.Application.DTOs.Properties;
using PropertyPortal.Application.Extensions;

namespace PropertyPortal.Application.Validators.Properties
{
    public class PropertyUpdateDtoValidator : AbstractValidator<PropertyUpdateDto>
    {
        public PropertyUpdateDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required for updates.");
            //this.ApplyLocatableRules();

            // Ensure RowVersion is present for the concurrency check
            RuleFor(x => x.RowVersion)
                .NotEmpty().WithMessage("Concurrency token (RowVersion) is missing.");

            //RuleFor(x => x.Id).NotEmpty();
            //// Include shared rules here or via a shared ValidatorBase
            //RuleFor(x => x.Description).MaximumLength(500);
            //// ... (Address rules)

            //// shared from propertycreate validator
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
