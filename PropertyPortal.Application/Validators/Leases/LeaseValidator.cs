using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.Application.Validators.Leases
{
    public class LeaseValidator : AbstractValidator<Lease>
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly IUnitOfWork _uow;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LeaseValidator(ITenantProvider tenantProvider, IUnitOfWork uow, IHttpContextAccessor httpContextAccessor)
        {
            _tenantProvider = tenantProvider;
            _uow = uow;
            _httpContextAccessor = httpContextAccessor;
            RuleFor(x => x.PropertyId)
            .NotEmpty()
            .MustAsync(async (lease, propertyId, cancellation) =>
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null) return false;

                // Admin bypass
                if (user.IsInRole("Admin")) return true;

                var userId = _tenantProvider.GetUserId();
                if (userId == null) return false;

                // Verify the Manager has access to this property
                // This is now a fast, direct lookup because Lease has PropertyId
                //return await _uow.PropertyManagers.Query()
                //    .AnyAsync(pm => pm.UserId == userId && pm.PropertyId == propertyId, cancellation);

                return await _uow.PropertyManagers.Query()
                    .AnyAsync(pm =>
                        pm.UserId == (Guid)userId &&
                        pm.PropertyId == propertyId,
                        cancellation);
            })
            .WithMessage("You are not authorized to manage leases for this property.");

            RuleFor(x => x.MonthlyRent)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Monthly rent cannot be negative.");

            RuleFor(x => x.StartDate).NotEmpty();

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .GreaterThan(x => x.StartDate)
                .WithMessage("Lease End Date must be after the Start Date.");

            RuleFor(x => x.ResidentId).NotEmpty();
            RuleFor(x => x.UnitId).NotEmpty();
        }
    }
}
