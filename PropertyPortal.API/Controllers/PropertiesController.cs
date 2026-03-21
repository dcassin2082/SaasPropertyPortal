using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.DTOs;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertiesController : ControllerBase
    {
        //private readonly ApplicationDbContext _context;
        //private readonly PropertyRepository _propertyRepo;
        //private readonly IBaseRepository<Property> _propertyRepo;
        private readonly IUnitOfWork _uow;

        public PropertiesController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Property>>> GetProperties()
        {
            // Use the UoW to access the Properties repository
            return await _uow.Properties.Query().ToListAsync();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProperty(Guid id, Property property)
        {
            if (id != property.Id) return BadRequest();

            // 1. Mark for update via UoW
            await _uow.Properties.PutAsync(id, property);

            // 2. The UoW handles the actual DB Save and Concurrency check
            await _uow.CompleteAsync();
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Property>> GetProperty(Guid id)
        {
            // ... your logic
            return await _uow.Properties.GetByIdAsync(id);
        }

        // POST: api/properties
        // TenantId and CreatedBy should be auto-populated by your SaveChanges override
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PropertyDto dto)
        {
            var property = new Property
            {
                Name = dto.Name,
                Address = dto.Address
                // Note: We are NOT setting TenantId or CreatedBy here!
            };

            await _uow.Properties.PostAsync(property);
            await _uow.CompleteAsync(); // This is where the magic (interception) happens

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
