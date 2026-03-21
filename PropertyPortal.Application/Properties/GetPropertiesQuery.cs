using MediatR;
using PropertyPortal.Application.DTOs;

namespace PropertyPortal.Application.Properties
{
    public record GetPropertiesQuery(Guid TenantId) : IRequest<List<PropertyDto>>;
}
