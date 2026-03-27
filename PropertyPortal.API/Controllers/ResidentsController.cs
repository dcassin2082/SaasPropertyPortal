using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.DTOs.Residents;
using PropertyPortal.Application.Extensions;
using PropertyPortal.Domain.Entities;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class ResidentsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ITenantProvider _tenantProvider;
    public ResidentsController(IUnitOfWork uow, ITenantProvider tenantProvider)
    {
        _uow = uow;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResidentResponseDto>>> GetResidents([FromQuery] string? search = null)
    {
        var userId = _tenantProvider.GetUserId() ?? Guid.Parse("3D91A36C-F52C-45B2-8B47-67B87303640A");
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Manager";

        //// 1. Check if the PropertyManager record is visible to EF
        //var managerLinkExists = await _uow.PropertyManagers.Query()
        //    .IgnoreQueryFilters() // Bypass Tenant filters for a second
        //    .AnyAsync(pm => pm.PropertyId == Guid.Parse("10AE7067-6F7E-48CE-B4BF-38EBBFE618C3")
        //                 && pm.UserId == userId);

        //Console.WriteLine($"Manager Link Visible: {managerLinkExists}");

        // ignoring query filters on both returned the results
        //var query = _uow.Residents.Query().Include(r => r.Property).AsQueryable();
        var query = _uow.Residents.Query().IgnoreQueryFilters().Include(r => r.Property).AsQueryable();

        // Use a simpler join for debugging
        query = query.Where(r => _uow.PropertyManagers.Query()
            //.IgnoreQueryFilters()
            .Any(pm => pm.PropertyId == r.PropertyId && pm.UserId == userId));


        if (userRole == "Manager")
        {
            query = query.Where(r => _uow.PropertyManagers.Query().Any(pm => pm.PropertyId == r.PropertyId && pm.UserId == userId));
        }
        query = query.ApplySearch(search);

        //var query = _uow.Residents.Query()
        //    .Include(r => r.Property)
        //    .ApplySearch(search); // Reuse your global search logic!

        var residents = await query.ToListAsync();
        return Ok(residents.Adapt<List<ResidentResponseDto>>());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResidentResponseDto>> GetResident(Guid id)
    {
        //var resident = await _uow.Residents.Query()
        //    .Include(r => r.Property)
        //    //.IgnoreQueryFilters()
        //    .FirstOrDefaultAsync(x => x.Id == id);

        var resident = await _uow.Residents.GetByIdAsync(id);
        if (resident == null) return NotFound();

        return Ok(resident.Adapt<ResidentResponseDto>());
    }

    [HttpPost]
    public async Task<ActionResult<ResidentResponseDto>> PostResident(ResidentRequestDto dto)
    {
        // 1. Map DTO to Entity
        // Mapster uses your Program.cs config to handle the Address complex type
        var resident = dto.Adapt<Resident>();

        // 2. Persist via BaseRepository
        // This triggers:
        // - Your ValidationFilter (running ResidentValidator + AddressValidator)
        // - Your BaseRepository logic (setting TenantId & syncing ILocatable.Name)
        await _uow.Residents.PostAsync(resident);

        // 3. Save to database
        await _uow.CompleteAsync();

        // 4. Return the Response DTO (this was NOT returning the propertyName)
        // We fetch it again or adapt the tracked entity to include the Property Name
        var response = resident.Adapt<ResidentResponseDto>();

        //// RE-FETCH with Include so Mapster can see the Property Name
        //var freshResident = await _uow.Residents.Query()
        //    .Include(r => r.Property)
        //    .FirstOrDefaultAsync(r => r.Id == resident.Id);
        //var response = freshResident.Adapt<ResidentResponseDto>();

        return CreatedAtAction(nameof(GetResident), new { id = resident.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutResident(Guid id, ResidentRequestDto dto)
    {
        // If this works, the problem is in a field NOT listed here (like RowVersion or CreatedBy)
        var resident = await _uow.Residents.Query().IgnoreQueryFilters()
            .Where(x => x.Id == id)
            .Select(x => new { x.FirstName, x.LastName })
            .FirstOrDefaultAsync();

        var existing = await _uow.Residents.GetByIdAsync(id);
        if (existing == null) return NotFound();

        // Update properties from DTO
        dto.Adapt(existing);

        await _uow.Residents.PutAsync(id, existing);
        await _uow.CompleteAsync();

        return NoContent();
    }

    // DELETE: api/Residents/id
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResident(Guid id)
    {
        await _uow.Residents.DeleteAsync(id);
        await _uow.CompleteAsync();
        return NoContent();
    }
}
