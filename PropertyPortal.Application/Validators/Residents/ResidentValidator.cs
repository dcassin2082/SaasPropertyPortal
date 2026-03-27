using FluentValidation;
using Microsoft.AspNetCore.Http;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.Extensions;
using PropertyPortal.Application.Validators.Common;
using PropertyPortal.Domain.Entities;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace PropertyPortal.Application.Validators.Residents
{
    public class ResidentValidator : AbstractValidator<Resident>
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly IUnitOfWork _uow;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ResidentValidator(ITenantProvider tenantProvider, IUnitOfWork uow, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _tenantProvider = tenantProvider;
            _uow = uow;
            RuleFor(x => x.PropertyId)
                .MustAsync(async (resident, propertyId, cancellation) =>
                {
                    // 1. Get User Info from Provider/Claims
                    var role = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
                    if (role == "Admin") return true;

                    var userId = _tenantProvider.GetUserId();
                    if (userId == null) return false;

                    // 2. Check if this specific Manager is linked to this specific Property
                    // You'll need a PropertyManagers repository or a raw query in your UoW
                    return await _uow.PropertyManagers.Query() // Access the IQueryable
                    .AnyAsync(pm => pm.UserId == (Guid)userId && pm.PropertyId == propertyId, cancellation);
                })
                .WithMessage("You are not authorized to manage residents for this property.");


            // 1. Basic Info
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));

            // 2. Financials
            RuleFor(x => x.RentAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Rent amount cannot be negative.");

            // 3. Lease Logic
            RuleFor(x => x.LeaseStartDate).NotEmpty();
            RuleFor(x => x.LeaseEndDate)
                .NotEmpty()
                .GreaterThan(x => x.LeaseStartDate)
                .WithMessage("Lease End Date must be after the Start Date.");

            // 4. Relations
            RuleFor(x => x.PropertyId).NotEmpty();
            RuleFor(x => x.UnitId).NotEmpty();

            // 5. The Complex Type Mapping
            // This triggers your existing AddressValidator for the nested Address record
            RuleFor(x => x.Address).SetValidator(new AddressValidator());

            // 6. Reusing your Shared Logic (ValidationExtensions - extends AbstractValidator<T>
            this.ApplyLocatableRules();         // Validates Name/Description for search
        }

        //private async Task<bool> BeAnAllowedProperty(Guid propertyId, CancellationToken cancellation)
        //{
        //    // 1. Admins bypass this check
        //    if (_tenantProvider.UserRole == "Admin") return true;

        //    // 2. Managers must have the PropertyId in their assigned list
        //    // (Assuming ITenantProvider exposes the IDs the JWT allows)
        //    var allowedProperties = _tenantProvider.GetAssignedPropertyIds();

        //    return allowedProperties.Contains(propertyId);
        //}
    }

}
