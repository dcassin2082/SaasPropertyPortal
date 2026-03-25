using Microsoft.AspNetCore.Mvc;
using PropertyPortal.Application.Common.Extensions;
using PropertyPortal.Application.Common.Models;

namespace PropertyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class ApiBaseController : ControllerBase
    {
        protected ActionResult<List<T>> OkPaged<T>(PaginatedResult<T> result)
        {
            Response.AddPaginationHeader(
                result.PageNumber,
                result.Items.Count, // Current page size
                result.TotalCount,
                result.TotalPages);

            return Ok(result.Items);
        }
    }
}
