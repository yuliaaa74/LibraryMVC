using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryMVC.Data;
using LibraryMVC.Models;
using LibraryMVC.DTOs;

namespace LibraryMVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthorsApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/AuthorsApi
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<Author>>> GetAuthors([FromQuery] int skip = 0, [FromQuery] int limit = 5)
        {
            var totalCount = await _context.Authors.CountAsync();

            var authors = await _context.Authors
                .OrderBy(a => a.Name)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            string? nextLink = null;
            if (skip + limit < totalCount)
            {
                nextLink = Url.Action(nameof(GetAuthors), null, new { skip = skip + limit, limit = limit }, Request.Scheme);
            }

            var response = new PaginatedResponse<Author>
            {
                Items = authors,
                TotalCount = totalCount,
                NextLink = nextLink
            };

            return Ok(response);
        }

        // POST: api/AuthorsApi
        [HttpPost]
        public async Task<ActionResult<Author>> PostAuthor(AuthorDto authorDto)
        {
            var newAuthor = new Author
            {
                Name = authorDto.Name,
                PhotoPath = authorDto.PhotoPath,
                Biography = authorDto.Biography,
                TenantId = authorDto.TenantId
            };

            _context.Authors.Add(newAuthor);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAuthor", new { id = newAuthor.Id }, newAuthor);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Author>> GetAuthor(int id)
        {
            var author = await _context.Authors.Include(a => a.Books).FirstOrDefaultAsync(a => a.Id == id);
            if (author == null) return NotFound();
            return author;
        }
        // PUT: api/AuthorsApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAuthor(int id, AuthorDto authorDto)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound();
            }

            author.Name = authorDto.Name;
            author.PhotoPath = authorDto.PhotoPath;
            author.Biography = authorDto.Biography;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/AuthorsApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuthor(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound();
            }

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
