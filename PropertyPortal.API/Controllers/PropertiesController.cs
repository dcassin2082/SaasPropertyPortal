using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.Common.Models;
using PropertyPortal.Application.DTOs.Properties;
using PropertyPortal.Application.Extensions;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PropertiesController : ApiBaseController
    {
        // properties controller
        //private readonly ApplicationDbContext _context;
        //private readonly PropertyRepository _propertyRepo;
        //private readonly IBaseRepository<Property> _propertyRepo;
        private readonly IUnitOfWork _uow;

        public PropertiesController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // added pagination to GetProperties & then added sorting
        [HttpGet]
        public async Task<PaginatedResult<PropertyResponseDto>> GetProperties(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "Name",
            [FromQuery] bool isDescending = false,
            [FromQuery] string? search = null)
        {
            // TEST ONLY: Does this work?

            // 1. Get the Queryable (Tenant filtering applied automatically)
            var query = _uow.Properties.Query().ApplySearch(search);

            //// TEST ONLY: Does this work? if this works the problem was in how Mapster was projecting
            //var rawProperties = await query.ToListAsync();
            //var dtos = rawProperties.Adapt<List<PropertyResponseDto>>();

            // 1. Manually project to ensure EF Core 8 ComplexProperty mapping is respected
            var projectedQuery = query.Select(p => new PropertyResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address, // EF8 knows how to map this "Address_" prefix here
                PropertyType = p.PropertyType,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy,
                IsDeleted = p.IsDeleted,
                Description = p.Description,
                // Calculate the aggregates here or keep them in Mapster if you prefer
                UnitCount = p.Units.Count(),
                TotalMonthlyRent = p.Units.Sum(u => u.Rent)
            });
            var properties = await _uow.Properties.GetPagedAsync(
                projectedQuery,
                pageNumber,
                pageSize,
                sortBy,
                isDescending);
            return properties;
            // 3. Pass everything to the Repo (Now handles Sorting + Pagination)
            //return await _uow.Properties.GetPagedAsync(
            //    projectedQuery,
            //    pageNumber,
            //    pageSize,
            //    sortBy,
            //    isDescending);
        }

        //// added pagination to GetProperties and added paging
        //public async Task<PaginatedResult<PropertyResponseDto>> GetProperties(int pageNumber, int pageSize)
        //{
        //    // 1. Get the Queryable from Repo
        //    var query = _uow.Properties.Query();

        //    // 2. Project to DTO (Mapster handles the UnitCount/Rent logic here)
        //    var projectedQuery = query.ProjectToType<PropertyResponseDto>();

        //    // 3. Apply Pagination and Execute
        //    return await _uow.Properties.GetPagedAsync(projectedQuery, pageNumber, pageSize);
        //}

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<PropertyResponseDto>>> GetProperties()
        //{
        //    //var properties = await _uow.Properties.Query()
        //    //    .Include(p => p.Units)
        //    //    .ToListAsync();
        //    /*  The "Pro" Performance Tip
        //            If you want this to be even faster and avoid the .Include() entirely, use Mapster's ProjectToType. 
        //        This does the Count and Sum directly in the SQL query (SQL COUNT and SUM) rather than pulling every unit into memory */

        //    // Mapster's ProjectToType writes the SQL for you!
        //    var properties = await _uow.Properties.Query()
        //        .ProjectToType<PropertyResponseDto>()
        //        .ToListAsync();

        //    return Ok(properties.Adapt<List<PropertyResponseDto>>());
        //}


        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyResponseDto>> GetProperty(Guid id)
        {
            var property = await _uow.Properties.GetByIdAsync(id);
            if (property == null)
                return NotFound();

            return Ok(property.Adapt<PropertyResponseDto>());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProperty(Guid id, PropertyUpdateDto dto)
        {
            var property = await _uow.Properties.GetByIdAsync(id);
            if (id != property.Id) return NotFound();

            // this is how we map the dto onto the existing entity without creating a new entity
            dto.Adapt(property);

            // 1. Mark for update via UoW
            await _uow.Properties.PutAsync(id, property);

            // 2. The UoW handles the actual DB Save and Concurrency check
            await _uow.CompleteAsync();
            return NoContent();
        }

        // POST: api/properties
        // TenantId and CreatedBy should be auto-populated by your SaveChanges override
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PropertyCreateDto dto)
        {
            var property = dto.Adapt<Property>();
            await _uow.Properties.PostAsync(property);
            await _uow.CompleteAsync();

            return CreatedAtAction(nameof(GetProperties), new { id = property.Id }, property);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProperty(Guid id)
        {
            // DeleteAsync in BaseRepo (accessed via UoW) handles the find and soft delete flag
            await _uow.Properties.DeleteAsync(id);
            await _uow.CompleteAsync();

            return NoContent();
        }
    }
}
