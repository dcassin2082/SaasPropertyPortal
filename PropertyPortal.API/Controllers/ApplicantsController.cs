using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.DTOs.Appliicants;
using PropertyPortal.Application.DTOs.Properties;
using PropertyPortal.Domain.Entities;
using PropertyPortal.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace PropertyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicantsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly IUnitOfWork _uow;

        public ApplicantsController(ApplicationDbContext context, ITenantProvider tenantProvider, IUnitOfWork uow)
        {
            _context = context;
            _tenantProvider = tenantProvider;
            _uow = uow;
        }

        // GET: api/Applicants
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ApplicantListResponseDto>>> GetApplicants([FromQuery] string? search = null,
                [FromQuery] Guid? propertyId = null,
                [FromQuery] string? status = null)
        {
            var userId = _tenantProvider.GetUserId();// ?? Guid.Parse("3D91A36C-F52C-45B2-8B47-67B87303640A");

            // 1. Get the list of Property IDs this user is allowed to manage 
            List<Guid?> allowedPropertyIds = await GetManagedPropertyIdsAsync((Guid)userId);

            // 2. Start the resident query using the pre-fetched IDs
            var applicants = _uow.Applicants.Query()
                .IgnoreQueryFilters()
                .Where(a => !a.IsDeleted && allowedPropertyIds.Contains(a.PropertyId));

            if (propertyId.HasValue && propertyId.Value != Guid.Empty)
            {
                applicants = applicants.Where(r => r.PropertyId == propertyId.Value);
            }

            applicants = status?.ToLower() switch
            {
                "approved" => applicants.Where(a => a.Status == "Approved"),                  
                "pending" => applicants.Where(a => a.Status == "Pending"),
                "converted" => applicants.Where(a => a.Status == "Converted"),
                _ => applicants
            };
            //var dto = applicants.Adapt<List<ApplicantResponseDto>>();

            var items = await applicants
                .Select(a => new ApplicantResponseDto
                {
                    PropertyId = a.PropertyId,           // 1. PropertyId
                    UnitId = a.UnitId,               // 2. UnitId
                    Id = a.Id,                   // 3. Id
                    FirstName = a.FirstName,            // 4. FirstName
                    LastName = a.LastName,             // ... and so on
                    Email = a.Email,
                    Phone = a.Phone,
                    CurrentStreet = a.CurrentStreet,
                    CurrentUnitNumber = a.CurrentUnitNumber,
                    CurrentCity = a.CurrentCity,
                    CurrentState = a.CurrentState,
                    CurrentZipCode = a.CurrentZipCode,
                    CreditScore = a.CreditScore,                   // CreditScore (if not in entity)
                    PropertyName = a.Property.Name,         // PropertyName
                    UnitNumber = a.Unit != null ? a.Unit.UnitNumber : "TBD",
                    ApplicationDate = a.ApplicationDate ?? DateTime.Now,
                    Status = a.Status
                })
                .ToListAsync();
            return Ok(new ApplicantListResponseDto
            {
                Applicants = items.OrderBy(a => a.FirstName),
                TotalCount = items.Count,
                ApprovedApplicants = items.Count(a => a.Status == "Approved"),
                AverageCreditScore = items.Count > 0 ? Convert.ToInt32((items.Sum(a => a.CreditScore) / items.Count)) : 0
            });
        }

        // GET: api/Applicants/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Applicant>> GetApplicant(Guid id)
        {
            var applicant = await _context.Applicants.FindAsync(id);

            if (applicant == null)
            {
                return NotFound();
            }

            return applicant;
        }

        // PUT: api/Applicants/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutApplicant(Guid id, Applicant applicant)
        {
            if (id != applicant.Id)
            {
                return BadRequest();
            }

            _context.Entry(applicant).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ApplicantExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Applicants
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ApplicantResponseDto>> Post([FromBody] CreateApplicantRequest request)
        {
            var applicant = new Applicant
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                PropertyId = request.PropertyId,
                UnitId = request.UnitId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                CurrentStreet = request.CurrentStreet,
                CurrentUnitNumber = request.CurrentUnitNumber,
                CurrentCity = request.CurrentCity,
                CurrentState = request.CurrentState,
                CurrentZipCode = request.CurrentZipCode,
                CreditScore = request.CreditScore,
                Status = "Pending"
                // RowVersion is handled automatically by SQL Server on Save
            };

            var result = await _uow.Applicants.PostAsync(applicant);
            await _uow.CompleteAsync();

            return Ok(result.Adapt<ApplicantResponseDto>());
        }

        private bool ApplicantExists(Guid id)
        {
            return _context.Applicants.Any(e => e.Id == id);
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
