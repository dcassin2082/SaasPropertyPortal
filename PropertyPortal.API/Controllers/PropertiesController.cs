using Humanizer;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.Common.Models;
using PropertyPortal.Application.DTOs.Properties;
using PropertyPortal.Application.Extensions;
using PropertyPortal.Domain.Entities;
using System.Text;

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
        private readonly ITenantProvider _tenantProvider;

        public PropertiesController(IUnitOfWork uow, ITenantProvider tenantProvider)
        {
            _uow = uow;
            _tenantProvider = tenantProvider;
        }

        [HttpGet("lookup")]
        public async Task<ActionResult<IEnumerable<PropertyLookupDto>>> GetPropertyLookup()
        {
            var userId = _tenantProvider.GetUserId();

            // Get only properties assigned to this manager
            var properties = await _uow.PropertyManagers.Query()
                //.IgnoreQueryFilters()
                .Where(pm => pm.UserId == userId)
                .Select(pm => new PropertyLookupDto
                {
                    Id = pm.PropertyId,
                    Name = pm.Property.Name,
                    ResidentCount = _uow.Residents.Query().IgnoreQueryFilters().Count(r => r.PropertyId == pm.PropertyId)
                })
                .Distinct()
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Ok(properties);
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
            var query = _uow.Properties.Query().ApplyPropertySearch(search);
            //// remove this and let the select do the work
            //query = query.Include(u => u.Units).Include(r => r.Residents);

            //// TEST ONLY: Does this work? if this works the problem was in how Mapster was projecting
            //var rawProperties = await query.ToListAsync();
            //var dtos = rawProperties.Adapt<List<PropertyResponseDto>>();

            // 1. Manually project to ensure EF Core 8 ComplexProperty mapping is respected
            var projectedQuery = query.Select(p => new PropertyResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Address1 = p.Address1, 
                Address2 = p.Address2, 
                City = p.City,
                State = p.State,
                ZipCode = p.ZipCode, 
                PropertyType = p.PropertyType,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy,
                IsDeleted = p.IsDeleted,
                // Calculate the aggregates here or keep them in Mapster if you prefer
                UnitCount = p.Units.Count(),
                ResidentCount = p.Residents.Count(),
                TotalMonthlyRent = p.Units.Where(u => !u.IsDeleted).Sum(u => u.Rent)
            });
            var properties = await _uow.Properties.GetPagedAsync(
                projectedQuery,
                pageNumber,
                pageSize,
                sortBy,
                isDescending);
            return properties;
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

        [HttpGet("{id}/stats")]
        public async Task<ActionResult<PropertyStatsDto>> GetPropertyStats(Guid id)
        {
            // 1. Fetch units and their occupancy status in one go
            var unitData = await _uow.Units.Query()
                .Where(u => u.PropertyId == id && !u.IsDeleted)
                .Select(u => new {
                    u.Rent,
                    IsOccupied = _uow.Residents.Query().Any(r => r.UnitId == u.Id && !r.IsDeleted)
                })
                .ToListAsync();

            if (!unitData.Any()) return Ok(new PropertyStatsDto(id, 0, 0, 0, 0, 0, 0));

            // 2. Calculate metrics
            var totalUnits = unitData.Count;
            var occupiedUnits = unitData.Count(u => u.IsOccupied);
            var potential = unitData.Sum(u => u.Rent);
            var actual = unitData.Where(u => u.IsOccupied).Sum(u => u.Rent);

            return Ok(new PropertyStatsDto(
                PropertyId: id,
                TotalPotentialRevenue: potential,
                ActualMonthlyRevenue: actual,
                OccupancyRate: Math.Round((double)occupiedUnits / totalUnits * 100, 1),
                TotalUnits: totalUnits,
                OccupiedUnits: occupiedUnits,
                AverageRent: totalUnits > 0 ? potential / totalUnits : 0
            ));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyResponseDto>> GetProperty(Guid id)
        {
            var property = await _uow.Properties.Query().Include(p => p.Units)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
                return NotFound();

            var dto = property.Adapt<PropertyResponseDto>();
            // Calculate IsOccupied for each unit
            foreach (var unitDto in dto.Units)
            {
                unitDto.IsOccupied = await _uow.Residents.Query()
                    .AnyAsync(r => r.UnitId == unitDto.Id && !r.IsDeleted);
            }

            return Ok(dto);
            //var property = await _uow.Properties.GetByIdAsync(id);
            //if (property == null)
            //    return NotFound();

            //return Ok(property.Adapt<PropertyResponseDto>());
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
