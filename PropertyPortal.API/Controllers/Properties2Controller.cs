using MediatR;
using Microsoft.AspNetCore.Mvc;
using PropertyPortal.Application.Properties;

namespace PropertyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Properties2Controller : ControllerBase
    {
        private readonly IMediator _mediator;

        public Properties2Controller(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var tenantId = Guid.Parse(HttpContext.Items["TenantId"].ToString());
            var result = await _mediator.Send(new GetPropertiesQuery(tenantId));
            return Ok(result);
        }
    }
}
