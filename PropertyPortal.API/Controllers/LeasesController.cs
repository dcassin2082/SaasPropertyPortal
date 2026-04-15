using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.DTOs.Appliicants;
using PropertyPortal.Application.DTOs.Leases;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeasesController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ITenantProvider _tenantProvider;

        public LeasesController(IUnitOfWork uow, ITenantProvider tenantProvider)
        {
            _uow = uow;
            _tenantProvider = tenantProvider;
        }

        // GET: api/Leases
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaseResponseDto>>> GetLeases([FromQuery] string? search = null,
                [FromQuery] Guid? propertyId = null,
                [FromQuery] string? status = null)
        {
            var userId = _tenantProvider.GetUserId(); // ?? Guid.Parse("3D91A36C-F52C-45B2-8B47-67B87303640A");

            if (userId == null || userId == Guid.Empty)
                return BadRequest();

            // 1. Get the list of Property IDs this user is allowed to manage 
            List<Guid?> allowedPropertyIds = await GetManagedPropertyIdsAsync((Guid)userId);

            // 2. Start the leases query using the pre-fetched IDs
            var query = _uow.Leases.Query()
                .Include(p => p.Resident)
                .ThenInclude(u => u.Unit)
                .IgnoreQueryFilters()
                .Where(l => !l.IsDeleted && allowedPropertyIds.Contains(l.PropertyId));
            /*public Guid Id { get; set; }

        public Guid ResidentId { get; set; }

        public Guid PropertyId { get; set; }

        public Guid UnitId { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public decimal MonthlyRent { get; set; }

        public decimal DepositAmount { get; set; }

        public string Status { get; set; } = null!;

        public decimal TotalPayments { get; set; } */

            var leases = await query.Select(l => new LeaseResponseDto
            {
                Id = l.Id,
                ResidentId = l.ResidentId,
                PropertyId = l.PropertyId,
                UnitId = l.UnitId,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                MonthlyRent = l.MonthlyRent,
                DepositAmount = l.DepositAmount,
                Status = l.Status,
                UnitNumber = l.Unit.UnitNumber,
                PropertyName = l.Property.Name,
                FirstName = l.Resident.FirstName,
                LastName = l.Resident.LastName
            }).ToListAsync();
            return Ok(new
            {
                Leases = leases,
                TotalCount = leases.Count,
                TotalRent = leases.Sum(l => l.MonthlyRent),
                TotalDeposits = leases.Sum(l => l.DepositAmount)
            });
        }

        // GET: api/Leases/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Lease>> GetLease(Guid id)
        {
            var lease = await _uow.Leases.GetByIdAsync(id);

            if (lease == null)
            {
                return NotFound();
            }

            return lease;
        }

        // PUT: api/Leases/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLease(Guid id, Lease lease)
        {
            if (id != lease.Id)
            {
                return BadRequest();
            }

            await _uow.Leases.PutAsync(id, lease);
            await _uow.CompleteAsync();

            return NoContent();
        }

        // POST: api/Leases
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Lease>> PostLease(Lease lease)
        {
            await _uow.Leases.PostAsync(lease);
            await _uow.CompleteAsync();
            return CreatedAtAction("GetLease", new { id = lease.Id }, lease);
        }

        // DELETE: api/Leases/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLease(Guid id)
        {
            await _uow.Leases.DeleteAsync(id);
            await _uow.CompleteAsync();

            return NoContent();
        }

        private async Task<List<Guid?>> GetManagedPropertyIdsAsync(Guid userId)
        {
            return await _uow.PropertyManagers.Query()
            .Where(pm => pm.UserId == userId)
            .Select(pm => pm.PropertyId)
            .ToListAsync();
        }
    }
}
