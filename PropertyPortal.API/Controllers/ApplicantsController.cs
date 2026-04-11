using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.DTOs.Appliicants;
using PropertyPortal.Domain.Entities;
using PropertyPortal.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task<ActionResult<IEnumerable<Applicant>>> GetApplicants()
        {
            return await _context.Applicants.ToListAsync();
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
        public async Task<ActionResult<ApplicantResponse>> Post([FromBody] CreateApplicantRequest request)
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

            return Ok(result.Adapt<ApplicantResponse>());
        }

        private bool ApplicantExists(Guid id)
        {
            return _context.Applicants.Any(e => e.Id == id);
        }
    }
}
