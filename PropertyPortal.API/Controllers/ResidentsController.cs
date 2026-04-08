using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.DTOs.Properties;
using PropertyPortal.Application.DTOs.Residents;
using PropertyPortal.Application.Extensions;
using PropertyPortal.Domain.Entities;
using System.Security.Claims;
using System.Text;

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
    public async Task<ActionResult<ResidentListResponse>> GetResidents(
    [FromQuery] string? search = null,
    [FromQuery] Guid? propertyId = null,
    [FromQuery] string? status = null)
    {
        var userId = _tenantProvider.GetUserId() ?? Guid.Parse("3D91A36C-F52C-45B2-8B47-67B87303640A");
        var now = DateTime.UtcNow;

        // 1. Get the list of Property IDs this user is allowed to manage FIRST
        List<Guid> allowedPropertyIds = await GetManagedPropertyIdsAsync(userId);
        // 2. Start the resident query using the pre-fetched IDs
        var query = _uow.Residents.Query()
            .IgnoreQueryFilters()
            .Where(r => allowedPropertyIds.Contains(r.PropertyId));

        //// Start with a base query of residents the manager is allowed to see
        //var query = _uow.Residents.Query()
        //    .IgnoreQueryFilters()
        //    .Where(r => _uow.PropertyManagers.Query()
        //        .Any(pm => pm.PropertyId == r.PropertyId && pm.UserId == userId));

        // If a specific property is selected in the dropdown, filter by it
        if (propertyId.HasValue && propertyId.Value != Guid.Empty)
        {
            query = query.Where(r => r.PropertyId == propertyId.Value);
        }

        // 2. Filter by Status (Lease Date Logic)
        if (!string.IsNullOrEmpty(status))
        {
            query = status.ToLower() switch
            {
                "active" => query.Where(r => r.LeaseStartDate <= DateOnly.FromDateTime(now) && r.LeaseEndDate >= DateOnly.FromDateTime(now)),
                "upcoming" => query.Where(r => r.LeaseStartDate > DateOnly.FromDateTime(now)),
                "past" => query.Where(r => r.LeaseEndDate < DateOnly.FromDateTime(now)),
                _ => query
            };
        }
        query = query.ApplyResidentSearch(search);
        /*System.Data.SqlTypes.SqlNullValueException: 'Data is Null. This method or property cannot be called on Null values.'
            */
        //var residents = await query
        //    .Include(r => r.Unit)
        //    .Include(r => r.Property)
        //    .ToListAsync();

        var residents = await query
        .Select(r => new ResidentResponseDto(
        r.Id,
        r.PropertyId,
        r.Property != null ? r.Property.Name : "Unassigned",
        r.UnitId,
        r.Unit != null ? r.Unit.UnitNumber : "N/A",
        $"{r.FirstName} {r.LastName}",
        r.Email,
        r.Phone,
        r.LeaseStartDate,
        r.LeaseEndDate,
        r.Unit != null ? r.Unit.Rent : 0,
        r.IsDeleted,
        // Pull the address from the PROPERTY, not the UNIT
        r.Property != null
            ? $"{r.Property.Address.Street}, {r.Property.Address.City}, {r.Property.Address.State}"
            : "No Address"
    ))
    .ToListAsync();
        return Ok(new ResidentListResponse
        {
            //Residents = residents.Adapt<List<ResidentResponseDto>>(),
            /*Since you already used .Select(r => new ResidentResponseDto(...)) in your LINQ query, your residents list is already 
             * a list of DTOs. Calling .Adapt (Mapster) again is redundant and costs extra CPU cycles. You can just do: */
            Residents = residents, // no need to adapt anything
            TotalCount = residents.Count,
            TotalMonthlyRent = residents.Where(r => !r.IsDeleted).Sum(r => r.RentAmount)
        });
    }

    [HttpGet("revenue-trends")]
    public async Task<ActionResult<IEnumerable<object>>> GetRevenueTrends()
    {
        var userId = _tenantProvider.GetUserId() ?? Guid.Parse("3D91A36C-F52C-45B2-8B47-67B87303640A");

        // Use DateOnly for comparison to match your Residents model properties
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var sixMonthsAgo = now.AddMonths(-6);

        // 1. Define the base filtered query
        var baseQuery = _uow.Residents.Query()
        .IgnoreQueryFilters()
        .Where(r => _uow.PropertyManagers.Query()
            .Any(pm => pm.PropertyId == r.PropertyId && pm.UserId == userId));

        // 2. Filter and Group
        var trends = await baseQuery
        .Where(r => r.LeaseStartDate <= now && r.LeaseEndDate >= sixMonthsAgo && !r.IsDeleted)
        // Grouping by Year and Month for chronological order
        .GroupBy(r => new { r.LeaseStartDate.Year, r.LeaseStartDate.Month })
        .Select(g => new
        {
            Year = g.Key.Year,
            Month = g.Key.Month,
            Revenue = g.Sum(r => r.RentAmount),
            Projected = g.Sum(r => r.RentAmount) * 1.1m
        })
        .ToListAsync();

        // 3. Final formatting for the Chart (handled in memory for clean string labels)
        var formattedTrends = trends
        .OrderBy(t => t.Year)
        .ThenBy(t => t.Month)
        .Select(t => new
        {
            Month = $"{t.Month}/{t.Year}", // "4/2026"
            Revenue = t.Revenue,
            Projected = t.Projected
        });

        return Ok(formattedTrends);
    }

    //[HttpGet]
    //public async Task<ActionResult<IEnumerable<ResidentResponseDto>>> GetResidents([FromQuery] string? search = null)
    //{
    //    var userId = _tenantProvider.GetUserId() ?? Guid.Parse("3D91A36C-F52C-45B2-8B47-67B87303640A");
    //    var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Manager";

    //    var query = _uow.Residents.Query().IgnoreQueryFilters().Include(r => r.Property).AsQueryable();

    //    // Use a simpler join for debugging
    //    query = query.Where(r => _uow.PropertyManagers.Query()
    //        //.IgnoreQueryFilters()
    //        .Any(pm => pm.PropertyId == r.PropertyId && pm.UserId == userId));


    //    if (userRole == "Manager")
    //    {
    //        query = query.Where(r => _uow.PropertyManagers.Query().Any(pm => pm.PropertyId == r.PropertyId && pm.UserId == userId));
    //    }
    //    query = query.ApplySearch(search);

    //    var residents = await query.Include(r => r.Unit).ThenInclude(u => u.Property).ToListAsync();

    //    var response = new ResidentListResponse
    //    {
    //        Residents = residents.Adapt<List<ResidentResponseDto>>(),
    //        TotalCount = residents.Count(),
    //        TotalMonthlyRent = residents.Sum(r => r.RentAmount)
    //    };

    //    return Ok(response);

    //    //return Ok(residents.Adapt<List<ResidentResponseDto>>());
    //}

    [HttpGet("export")]
    public async Task<IActionResult> ExportResidents(
    [FromQuery] string? search = null,
    [FromQuery] Guid? propertyId = null,
    [FromQuery] string? status = null)
    {
        var userId = _tenantProvider.GetUserId() ?? Guid.Parse("3D91A36C-F52C-45B2-8B47-67B87303640A");
        var now = DateTime.UtcNow;

        // Use the SAME filtering logic as your GetResidents method
        var query = _uow.Residents.Query()
        .IgnoreQueryFilters()
        .Where(r => _uow.PropertyManagers.Query()
            .Any(pm => pm.PropertyId == r.PropertyId && pm.UserId == userId));

        if (propertyId.HasValue && propertyId.Value != Guid.Empty)
            query = query.Where(r => r.PropertyId == propertyId.Value);

        if (!string.IsNullOrEmpty(status))
        {
            query = status.ToLower() switch
            {
                "active" => query.Where(r => r.LeaseStartDate <= DateOnly.FromDateTime(now) && r.LeaseEndDate >= DateOnly.FromDateTime(now)),
                "upcoming" => query.Where(r => r.LeaseStartDate > DateOnly.FromDateTime(now)),
                "past" => query.Where(r => r.LeaseEndDate < DateOnly.FromDateTime(now)),
                _ => query
            };
        }

        var residents = await query.Include(r => r.Property).ToListAsync();

        // Generate CSV String
        var csv = new StringBuilder();
        csv.AppendLine("FirstName,LastName,Property,RentAmount,LeaseEnd");

        foreach (var r in residents)
        {
            csv.AppendLine($"{r.FirstName},{r.LastName},{r.Property.Name},{r.RentAmount},{r.LeaseEndDate:yyyy-MM-dd}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"Residents_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpPost("bulk-renew")]
    public async Task<IActionResult> BulkRenewLeases([FromBody] BulkRenewalRequest request)
    {
        var userId = _tenantProvider.GetUserId();

        // 1. Fetch only residents that belong to this manager
        var residents = await _uow.Residents.Query()
        .Where(r => request.ResidentIds.Contains(r.Id))
        .Where(r => _uow.PropertyManagers.Query()
            .Any(pm => pm.PropertyId == r.PropertyId && pm.UserId == userId))
        .ToListAsync();

        foreach (var resident in residents)
        {
            // 2. Apply Rent Logic
            if (request.PercentIncrease.HasValue)
                resident.RentAmount *= (decimal)(1 + (request.PercentIncrease.Value / 100));
            else if (request.NewRentAmount.HasValue)
                resident.RentAmount = request.NewRentAmount.Value;

            // 3. Apply Date Logic
            resident.LeaseStartDate = DateOnly.FromDateTime(DateTime.UtcNow);
            resident.LeaseEndDate = DateOnly.FromDateTime(request.NewEndDate);
            await _uow.Residents.PutAsync(resident.Id, resident);
        }

        await _uow.CompleteAsync();
        return Ok(new { Count = residents.Count });
    }

    [HttpPost("bulk-notice")]
    public async Task<IActionResult> SendBulkNotice([FromBody] BulkNoticeRequest request)
    {
        // request.ResidentIds will be your selectedIds array
        // request.MessageTemplate would be "LeaseRenewal" etc.

        // 1. Fetch the residents by the list of IDs
        // 2. Filter them again by UserId to ensure security (don't trust the client!)
        // 3. Trigger your Email/Notification service

        return Ok(new { Message = $"Sent notices to {request.ResidentIds.Count} residents." });
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

    private async Task<List<Guid>> GetManagedPropertyIdsAsync(Guid userId)
    {
        return await _uow.PropertyManagers.Query()
        .Where(pm => pm.UserId == userId)
        .Select(pm => pm.PropertyId)
        .ToListAsync();
    }
}
