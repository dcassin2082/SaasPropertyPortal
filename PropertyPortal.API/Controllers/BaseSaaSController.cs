using Microsoft.AspNetCore.Mvc;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Domain.Common;
using PropertyPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PropertyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseSaaSController<TEntity> : ControllerBase where TEntity : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly ITenantProvider _tenantProvider;

        public BaseSaaSController(ApplicationDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        [HttpGet]
        public virtual async Task<ActionResult<IEnumerable<TEntity>>> GetAll()
        {
            // Global Query Filters (IsDeleted/TenantId) are applied here automatically
            return await _context.Set<TEntity>().ToListAsync();
        }

        [HttpGet("{id}")]
        public virtual async Task<ActionResult<TEntity>> GetById(Guid id)
        {
            var entity = await _context.Set<TEntity>().FindAsync(id);
            if (entity == null) return NotFound();
            return entity;
        }

        [HttpPost]
        public virtual async Task<ActionResult<TEntity>> Create(TEntity entity)
        {
            // AuditInterceptor handles CreatedAt/CreatedBy/TenantId
            _context.Set<TEntity>().Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public virtual async Task<IActionResult> Update(Guid id, TEntity entity)
        {
            if (id != entity.Id) return BadRequest("ID mismatch");

            // EF Core tracks the RowVersion here. If it doesn't match the DB, 
            // an exception is thrown on SaveChanges.
            _context.Entry(entity).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "The record was modified by another user. Please reload." });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.Set<TEntity>().FindAsync(id);
            if (entity == null) return NotFound();

            // Soft delete: Global Query Filter will hide this record from now on
            entity.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
