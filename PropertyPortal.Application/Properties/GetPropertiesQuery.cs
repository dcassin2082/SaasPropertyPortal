using MediatR;
using PropertyPortal.Application.DTOs.Properties;

namespace PropertyPortal.Application.Properties
{
    public record GetPropertiesQuery(Guid TenantId) : IRequest<List<PropertyResponseDto>>;
}
