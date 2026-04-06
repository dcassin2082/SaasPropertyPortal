using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Domain.Entities;
using PropertyPortal.Infrastructure.Data;

namespace PropertyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResidentNotesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ResidentNotesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ResidentNotes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResidentNote>>> GetResidentNotes()
        {
            return await _context.ResidentNotes.ToListAsync();
        }

        [HttpGet("{id}/notes")]
        public async Task<ActionResult<IEnumerable<ResidentNote>>> GetNotes(Guid id)
        {
            var notes = await _context.ResidentNotes
                .Where(n => n.ResidentId == id)
                .OrderByDescending(n => n.CreatedAt)
                .AsNoTracking() // Performance win!
                .ToListAsync();

            return Ok(notes);
        }

        // GET: api/ResidentNotes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ResidentNote>> GetResidentNote(Guid id)
        {
            var residentNote = await _context.ResidentNotes.FindAsync(id);

            if (residentNote == null)
            {
                return NotFound();
            }

            return residentNote;
        }

        // PUT: api/ResidentNotes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutResidentNote(Guid id, ResidentNote residentNote)
        {
            if (id != residentNote.Id)
            {
                return BadRequest();
            }

            _context.Entry(residentNote).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResidentNoteExists(id))
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

        [HttpGet("activity")]
        public async Task<ActionResult<IEnumerable<ResidentNote>>> GetRecentActivity()
        {
            var activity = await _context.ResidentNotes
                .Include(n => n.Resident) // We need the name to show "John Doe: [Note Content]"
                .OrderByDescending(n => n.CreatedAt)
                .Take(15)
                .AsNoTracking()
                .Select(n => new {
                    n.Id,
                    n.Content,
                    n.CreatedAt,
                    ResidentName = n.Resident.FirstName + " " + n.Resident.LastName,
                    n.ResidentId
                })
                .ToListAsync();

            return Ok(activity);
        }

        [HttpPost("{residentId}/notes")]
        public async Task<IActionResult> AddNote(Guid residentId, [FromBody] string content)
        {
            var note = new ResidentNote
            {
                ResidentId = residentId,
                Content = content
            };

            _context.ResidentNotes.Add(note);
            await _context.SaveChangesAsync();

            return Ok(note);
        }

        // POST: api/ResidentNotes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ResidentNote>> PostResidentNote(ResidentNote residentNote)
        {
            _context.ResidentNotes.Add(residentNote);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetResidentNote", new { id = residentNote.Id }, residentNote);
        }

        // DELETE: api/ResidentNotes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResidentNote(Guid id)
        {
            var residentNote = await _context.ResidentNotes.FindAsync(id);
            if (residentNote == null)
            {
                return NotFound();
            }

            _context.ResidentNotes.Remove(residentNote);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ResidentNoteExists(Guid id)
        {
            return _context.ResidentNotes.Any(e => e.Id == id);
        }
    }
}
