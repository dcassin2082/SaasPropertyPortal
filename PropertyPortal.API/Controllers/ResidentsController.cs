using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.DTOs.Properties;
using PropertyPortal.Application.DTOs.Residents;
using PropertyPortal.Application.Extensions;
using PropertyPortal.Domain.Entities;
using PropertyPortal.Domain.Enums;
using System.Security.Claims;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class ResidentsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ResidentsController> _logger;
    public ResidentsController(IUnitOfWork uow, ITenantProvider tenantProvider, ILogger<ResidentsController> logger)
    {
        _uow = uow;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ResidentListResponse>> GetResidents(
    [FromQuery] string? search = null,
    [FromQuery] Guid? propertyId = null,
    [FromQuery] string? status = null)
    {
        var userId = _tenantProvider.GetUserId() ?? Guid.Parse("3D91A36C-F52C-45B2-8B47-67B87303640A");

        // 1. Get the list of Property IDs this user is allowed to manage FIRST
        List<Guid?> allowedPropertyIds = await GetManagedPropertyIdsAsync(userId);

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
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        query = status.ToLower() switch
        {
            // Residents who have a lease that covers today
            "active" => query.Where(r => r.Leases.Any(l =>
                l.Status == "Active" &&
                l.StartDate <= now &&
                l.EndDate >= now)),

            // Residents who have a lease starting in the future, but no currently active ones
            "upcoming" => query.Where(r => r.Leases.Any(l => l.StartDate > now) &&
                                        !r.Leases.Any(l => l.StartDate <= now && l.EndDate >= now)),

            // Residents where all leases are in the past
            "past" => query.Where(r => r.Leases.All(l => l.EndDate < now) && r.Leases.Any()),

            _ => query
        };
        query = query.ApplyResidentSearch(search);
        //var residents = await query
        //    .AsNoTracking() // Important: ignore local cache
        //    .Select(r => new {
        //        Res = r,
        //        // Specifically look for the lease linked by ResidentId
        //        Lease = r.Leases
        //            .Where(l => l.Status == "Active" && !l.IsDeleted)
        //            //.Where(l => l.StartDate <= now && l.EndDate >= now)
        //            .OrderByDescending(l => l.CreatedAt)
        //            .FirstOrDefault()
        //})
        //.Select(x => new ResidentResponseDto(
        //    x.Res.Id,
        //    x.Res.PropertyId,
        //    x.Res.Property != null ? x.Res.Property.Name : "Unassigned",
        //    x.Res.UnitId,
        //    x.Res.Unit != null ? x.Res.Unit.UnitNumber : "N/A",
        //    $"{x.Res.FirstName} {x.Res.LastName}",
        //    x.Res.Email,
        //    x.Res.Phone,
        //    // If Lease exists, use Lease dates; otherwise, use Resident dates
        //    x.Lease != null ? x.Lease.StartDate : x.Res.LeaseStartDate,
        //    x.Lease != null ? x.Lease.EndDate : x.Res.LeaseEndDate,
        //    // If Lease exists, use Lease rent; otherwise, return 0
        //    x.Lease != null ? x.Lease.MonthlyRent : 0,
        //    x.Res.IsDeleted,
        //    x.Res.Property != null
        //        ? $"{x.Res.Property.Address.Street}, {x.Res.Property.Address.City}, {x.Res.Property.Address.State}"
        //        : "No Address",
        //    x.Lease != null ? x.Lease.Status : x.Res.Status
        //))
        //.ToListAsync();
        var residents = await query
    .AsNoTracking()
    .Select(r => new {
        Resident = r,
        ActiveLease = r.Leases
            .Where(l => l.Status == "Active" && !l.IsDeleted)
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefault()
    })
    // Flatten the data immediately into primitive types or the DTO
    .Select(x => new ResidentResponseDto(
        x.Resident.Id,
        x.Resident.PropertyId,
        x.Resident.Property != null ? x.Resident.Property.Name : "Unassigned",
        x.Resident.UnitId,
        x.Resident.Unit != null ? x.Resident.Unit.UnitNumber : "N/A",
        $"{x.Resident.FirstName} {x.Resident.LastName}",
        x.Resident.Email,
        x.Resident.Phone,
        x.ActiveLease != null ? x.ActiveLease.StartDate : x.Resident.LeaseStartDate,
        x.ActiveLease != null ? x.ActiveLease.EndDate : x.Resident.LeaseEndDate,
        x.ActiveLease != null ? x.ActiveLease.MonthlyRent : 0,
        x.Resident.IsDeleted,
        x.Resident.Property != null
            ? $"{x.Resident.Property.Address.Street}, {x.Resident.Property.Address.City}, {x.Resident.Property.Address.State}"
            : "No Address",
        x.ActiveLease != null ? x.ActiveLease.Status : x.Resident.Status
    ))
    .ToListAsync();

        var testLeaseCount = await _uow.Leases.Query().CountAsync();
        Console.WriteLine($"Total Leases in DB: {testLeaseCount}");
        var res = residents;
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

    [HttpGet("unassigned")]
    public async Task<ActionResult<IEnumerable<UnassignedResidentDto>>> GetUnassignedResidents()
    {
        // Lead Tip: Log the request to track how often users are looking for unassigned residents
        _logger.LogInformation("Fetching all unassigned residents for move-in workflow.");

        var residents = await _uow.Residents.Query()
            .Where(r => r.UnitId == null && !r.IsDeleted)
            .OrderBy(r => r.LastName)
            .Select(r => new UnassignedResidentDto
            {
                Id = r.Id,
                FirstName = r.FirstName,
                LastName = r.LastName,
                Email = r.Email,
                Status = r.Status ?? "Pending"
            })
        .ToListAsync();

        return Ok(residents);
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
    //[HttpPost]
    //public async Task<IActionResult> CreateResident([FromBody] CreateResidentRequestDto dto)
    //{
    //    // 1. Create the Resident (The Person)
    //    var resident = new Resident
    //    {
    //        FirstName = dto.FirstName,
    //        LastName = dto.LastName,
    //        PropertyId = dto.PropertyId,
    //        UnitId = dto.UnitId,
    //        Status = "Current", // Or "Approved"
    //        Address = dto.CurrentAddress // Their mailing address
    //    };

    //    await _uow.Residents.PostAsync(resident);

    //    // 2. Create the Lease (The Contract)
    //    var lease = new Lease
    //    {
    //        ResidentId = resident.Id, // Link to the ID we just created
    //        UnitId = dto.UnitId,
    //        StartDate = dto.StartDate,
    //        EndDate = dto.EndDate,
    //        MonthlyRent = dto.RentAmount,
    //        Status = "Active"
    //    };

    //    await _uow.Leases.PostAsync(lease);
    //    await _uow.SaveChangesAsync();

    //    return Ok(resident);
    //}

    public async Task MoveInTenant(Guid applicantId)
    {
        var applicant = await _uow.Applicants.GetByIdAsync(applicantId);

        // 1. Create Resident from Applicant data
        var resident = new Resident
        {
            FirstName = applicant.FirstName,
            LastName = applicant.LastName,
            UnitId = applicant.UnitId,
            PropertyId = applicant.PropertyId,
            Status = "Current"
        };
        await _uow.Residents.PostAsync(resident);

        // 2. Create the Lease (using data from the Wizard UI)
        var lease = new Lease
        {
            ResidentId = resident.Id,
            UnitId = applicant.UnitId,
            //StartDate = wizardDto.StartDate,
            //EndDate = wizardDto.EndDate,
            //MonthlyRent = wizardDto.Rent
        };
        await _uow.Leases.PostAsync(lease);

        // 3. Update the Unit Status
        var unit = await _uow.Units.GetByIdAsync(applicant.UnitId);
        unit.Status = "Occupied";

        // 4. Cleanup
        applicant.Status = "Converted"; // Or delete the applicant record

        await _uow.CompleteAsync();
    }

    [HttpPost]
    public async Task<ActionResult<ResidentResponseDto>> PostResident([FromBody] ResidentRequestDto dto)
    {
        // need to get name, contact info stuff from the applicant
        // put an applicants drop down on the Add New Resident form and pass that id
        // for start, end dates we already have date-pickers 

        // QUESTION: should we let the manager set the rent on the react form or should it come from unit?
        //              rent values change constantly, also change based on the length of the lease

        // Test Guid - remove after setting up front end
        Guid applicantId = Guid.Parse("8861C6BA-E5BD-4961-9B1E-29CA17C63A44");
        var applicant = await _uow.Applicants.GetByIdAsync(applicantId);

        // 1. Create Resident from Applicant data
        var resident = new Resident
        {
            FirstName = applicant.FirstName,
            LastName = applicant.LastName,
            Email = applicant.Email,
            Phone = dto.Phone, // using dto for now, need to add to applicant table, after resident is created this stuff will be edited in residents table
            UnitId = applicant.UnitId,
            PropertyId = applicant.PropertyId,
            Status = "Current"
        };
        await _uow.Residents.PostAsync(resident);
        var userId = _tenantProvider.GetUserId();
        // 2. Create the Lease (using data from the Wizard UI)
        var lease = new Lease
        {
            ResidentId = resident.Id,
            UnitId = applicant.UnitId,
            StartDate = dto.LeaseStartDate,
            EndDate = dto.LeaseEndDate,
            MonthlyRent = dto.RentAmount,
            UserId = (Guid)userId,
            Status = "Active"
            //StartDate = wizardDto.StartDate,
            //EndDate = wizardDto.EndDate,
            //MonthlyRent = wizardDto.Rent
        };
        await _uow.Leases.PostAsync(lease);

        // 3. Update the Unit Status
        var unit = await _uow.Units.GetByIdAsync(applicant.UnitId);
        unit.Status = "Occupied";

        // 4. Cleanup
        applicant.Status = "Converted"; // Or delete the applicant record

        await _uow.CompleteAsync();
       
        var freshResident = await _uow.Residents.Query()
            .Include(r => r.Property)
            .Include(l => l.Leases)
            .FirstOrDefaultAsync(r => r.Id == resident.Id);
        var response = freshResident.Adapt<ResidentResponseDto>();

        return CreatedAtAction(nameof(GetResident), new { id = resident.Id }, response);
    }

    [HttpPost("move-in")]
    public async Task<IActionResult> MoveInResident([FromBody] MoveInRequest request)
    {
        using var transaction = await _uow.BeginTransactionAsync();

        try
        {
            var resident = await _uow.Residents.GetByIdAsync(request.ResidentId);
            var unit = await _uow.Units.GetByIdAsync(request.UnitId);

            if (resident == null || unit == null) return NotFound();

            // 1. Create the Lease Record (The Financial Truth)
            var newLease = new Lease
            {
                Id = Guid.NewGuid(),
                TenantId = resident.Id, // Your FK to the Resident
                UnitId = unit.Id,
                UserId = _tenantProvider.GetUserId() ?? Guid.Empty,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                MonthlyRent = unit.Rent, // Snapshot the current rent price!
                DepositAmount = request.DepositAmount,
                Status = "Active"
            };

            await _uow.Leases.PostAsync(newLease);

            // 2. Update the Resident (The Occupancy Link)
            resident.UnitId = unit.Id;
            // Optional: Keep these in sync if you're still using the Resident columns for now
            resident.LeaseStartDate = request.StartDate;
            resident.LeaseEndDate = request.EndDate;

            // 3. Update the Unit (The Inventory Status)
            unit.Status = "Occupied";

            await _uow.CompleteAsync();
            await transaction.CommitAsync();

            return Ok();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Transaction failed");
        }
    }

    [HttpPatch("{residentId}/move-in/{unitId}")]
    public async Task<IActionResult> MoveIn(Guid residentId, Guid unitId)
    {
        // Start a Transaction (Very important for Lead roles)
        using var transaction = await _uow.BeginTransactionAsync();

        try
        {
            var resident = await _uow.Residents.GetByIdAsync(residentId);
            //var targetUnit = await _uow.Units.GetByIdAsync(unitId);

            // Lead-level Validation: Is the resident already in another unit?
            if (resident?.UnitId != null)
            {
                // This turns a "Move-In" into a "Transfer"
                _logger.LogInformation("Resident {id} is transferring units.", residentId);
            }

            resident?.UnitId = unitId;
            resident?.Status = "Active";

            // Update the Address record on the resident to match the new unit
            var targetUnit = await _uow.Units.Query()
                .Include(u => u.Property) // Include this if you need to check Property rules
                .FirstOrDefaultAsync(u => u.Id == unitId);

            if (targetUnit == null) return NotFound("Unit not found.");

            // 2. Perform your logic now that you have the object
            //targetUnit.Status = "Occupied";
            targetUnit.Status = UnitStatus.Occupied.ToString();

            resident?.Address = new Address(
                targetUnit?.Property.Address.Street,
                targetUnit?.UnitNumber,
                targetUnit?.Property.Address.City,
                targetUnit?.Property.Address.State,
                targetUnit?.Property.Address.ZipCode
            );

            if (targetUnit is not null)
                await _uow.Units.PutAsync(unitId, targetUnit);
            await _uow.CompleteAsync();
            await transaction.CommitAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Transaction failed during Move-In.");
        }
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

    private async Task<List<Guid?>> GetManagedPropertyIdsAsync(Guid userId)
    {
        return await _uow.PropertyManagers.Query()
        .Where(pm => pm.UserId == userId)
        .Select(pm => pm.PropertyId)
        .ToListAsync();
    }
}
