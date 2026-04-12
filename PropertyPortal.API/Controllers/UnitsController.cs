using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.DTOs.Units;
using PropertyPortal.Domain.Entities;
using PropertyPortal.Domain.Enums;
using System.Linq;

namespace PropertyPortal.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UnitsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ITenantProvider _tenantProvider;

        public UnitsController(IUnitOfWork uow, ITenantProvider tenantProvider)
        {
            _uow = uow;
            _tenantProvider = tenantProvider;
        }

        // GET: api/Units
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<UnitResponseDto>>> GetUnits()
        //{
        //    //var units = await _uow.Units.Query().IgnoreQueryFilters().Include(u => u.Property).ToListAsync();
        //    //return Ok(units.Adapt<List<UnitResponseDto>>());
        //    var units = await _uow.Units.Query()
        //.IgnoreQueryFilters()
        //.Select(u => new UnitResponseDto
        //{
        //    Id = u.Id,
        //    UnitNumber = u.UnitNumber ?? "N/A", // Handle potential nulls
        //    Rent = u.Rent,
        //    PropertyId = u.PropertyId,
        //    PropertyName = u.Property != null ? u.Property.Name : "Unassigned"
        //})
        //.ToListAsync();

        //    return Ok(units);
        //    //// Map the list of Entities to a list of DTOs
        //    //var response = units.Select(u => new UnitResponseDto
        //    //{
        //    //    Id = u.Id,
        //    //    UnitNumber = u.UnitNumber,
        //    //    Description = u.Description,
        //    //    PropertyId = u.PropertyId,
        //    //    CreatedAt = u.CreatedAt,
        //    //    Bedrooms = u.Bedrooms.HasValue ? (int)u.Bedrooms : 0,
        //    //    Bathrooms = u.Bathrooms.HasValue ? (int)u.Bathrooms : 0,
        //    //    Rent = u.Rent
        //    //}).ToList();

        //    //return Ok(response);
        //}
        public async Task<ActionResult<IEnumerable<UnitResponseDto>>> GetUnits([FromQuery] Guid? propertyId)
        {
            // Filter by propertyId if it exists, otherwise keep it empty for safety
            if (!propertyId.HasValue) return Ok(new List<UnitResponseDto>());
            
            var units = await _uow.Units.Query()
                .Where(u => u.PropertyId == propertyId.Value)
                .Select(u => new UnitResponseDto
                {
                    Id = u.Id,
                    UnitNumber = u.UnitNumber,
                    Description = u.Description,
                    Bedrooms = u.Bedrooms,   // SQL Nulls map to int? perfectly here
                    Bathrooms = u.Bathrooms, // SQL Nulls map to int? perfectly here
                    Rent = u.Rent,
                    CreatedAt = u.CreatedAt,
                    PropertyId = u.PropertyId,
                    PropertyName = u.Property.Name,
                    TenantName = _uow.Residents.Query().Where(r => r.UnitId == u.Id && !r.IsDeleted)
                    .Where(r => !string.IsNullOrWhiteSpace(r.FirstName))
                    .Select(r => r.FirstName + " " + r.LastName).FirstOrDefault(),
                    IsOccupied = _uow.Residents.Query().Any(r => r.UnitId == u.Id && !r.IsDeleted)
                })
                .ToListAsync();

            return Ok(units);
        }
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<UnitResponseDto>>> GetUnits([FromQuery] Guid? propertyId)
        //{
        //    var query = _uow.Units.Query();

        //    if (propertyId.HasValue)
        //    {
        //        query = query.Where(u => u.PropertyId == propertyId.Value);
        //    }

        //    var units = await query
        //        .Select(u => new UnitResponseDto
        //        {
        //            Id = u.Id,
        //            UnitNumber = u.UnitNumber
        //        })
        //        .ToListAsync();

        //    return Ok(units);
        //}

        // GET: api/Units/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UnitResponseDto>> GetUnit(Guid id)
        {
            //var unit = await _uow.Units.GetByIdAsync(id);
            var unit = await _uow.Units.Query().Include(u => u.Property).FirstOrDefaultAsync(u => u.Id == id);
            if (unit == null)
            {
                return NotFound();
            }
            return Ok(unit.Adapt<UnitResponseDto>());

            //// Mapping to DTO breaks the 'Property -> Unit -> Property' cycle
            //return Ok(new UnitResponseDto
            //{
            //    Id = unit.Id,
            //    UnitNumber = unit.UnitNumber,
            //    Description = unit.Description,
            //    PropertyId = unit.PropertyId,
            //    CreatedAt = unit.CreatedAt,
            //    Bedrooms = unit.Bedrooms.HasValue ? (int)unit.Bedrooms : 0,
            //    Bathrooms = unit.Bathrooms.HasValue ? (int)unit.Bathrooms : 0,
            //    Rent = unit.Rent
            //});
        }

        [HttpPost("bulk")]
        public async Task<ActionResult> BulkCreate([FromBody] UnitBulkCreateDto dto)
        {
            var property = await _uow.Properties.GetByIdAsync(dto.PropertyId);
            if (property == null) return NotFound("Property not found");

            var newUnits = new List<Unit>();

            var existingUnitNumbers = await _uow.Units.Query()
                .Where(u => u.PropertyId == dto.PropertyId)
                .Select(u => u.UnitNumber)
                .ToListAsync();

            for (int i = 0; i < dto.Count; i++)
            {
                var unitNumber = (dto.StartingNumber + i).ToString();

                // Skip if it already exists
                if (existingUnitNumbers.Contains(unitNumber)) continue;

                newUnits.Add(new Unit
                {
                    PropertyId = dto.PropertyId,
                    UnitNumber = unitNumber,
                    Rent = dto.BaseRent,
                    UnitType = dto.UnitType ?? "Standard",
                    TenantId = _tenantProvider.GetTenantId(), // Ensure multi-tenancy
                    Status = UnitStatus.Vacant.ToString(),
                    //Address = new Address(
                    //        property.Address.Street,
                    //        unitNumber, // This sets the specific Unit # for this address
                    //        property.Address.City,
                    //        property.Address.State,
                    //        property.Address.ZipCode
                    //    )
                    //Address =
                    //{
                    //    Street = "",
                    //    UnitNumber = "",
                    //    City = "",
                    //    State = "",
                    //    ZipCode

                    //}
                });
            }

            await _uow.Units.AddRangeAsync(newUnits);
            await _uow.CompleteAsync();

            return Ok(new { count = newUnits.Count });
        }

        // PUT: api/Units/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUnit(Guid id, UnitUpdateDto dto)
        {
            var unit = await _uow.Units.GetByIdAsync(id);

            if (id != unit.Id)
            {
                return BadRequest();
            }

            //// Only update allowed fields (you can add more to the dto if needed)
            //unit.UnitNumber = dto.UnitNumber;
            //unit.Description = dto.Description;
            //unit.Bathrooms = dto.Bathrooms;
            //unit.Bedrooms = dto.Bedrooms;
            //unit.Rent = dto.Rent;

            dto.Adapt(unit);
            await _uow.Units.PutAsync(id, unit);

            await _uow.CompleteAsync();

            return NoContent();
        }

        // POST: api/Units
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<UnitCreateDto>> PostUnit(UnitCreateDto dto)
        {
            var property = await _uow.Properties.GetByIdAsync(dto.PropertyId);
            if (property == null)
                return NotFound("Property not found in your account");

            //var unit = new Unit
            //{
            //    Id = Guid.NewGuid(),
            //    UnitNumber = dto.UnitNumber,
            //    Description = dto.Description,
            //    PropertyId = dto.PropertyId,
            //    Bathrooms = dto.Bathrooms,
            //    Bedrooms = dto.Bedrooms,
            //    Rent = dto.Rent
            //    // TenantId & CreatedBy are handled by SaveChanges override!
            //};

            var unit = dto.Adapt<Unit>();
            await _uow.Units.PostAsync(unit);
            await _uow.CompleteAsync();

            // Map to Response DTO to break the circular reference (using Mapster)
            var response = unit.Adapt<UnitResponseDto>();

            // Map to Response DTO to break the circular reference (manually)
            //var response = new UnitResponseDto
            //{
            //    Id = unit.Id,
            //    UnitNumber = unit.UnitNumber,
            //    Description = unit.Description,
            //    PropertyId = unit.PropertyId,
            //    CreatedAt = unit.CreatedAt
            //};

            return CreatedAtAction("GetUnit", new { id = response.Id }, response);
        }

        // DELETE: api/Units/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUnit(Guid id)
        {
            await _uow.Units.DeleteAsync(id);
            await _uow.CompleteAsync();
            return NoContent();
        }
    }
}
