using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryMVC.Data;
using LibraryMVC.DTOs;
using LibraryMVC.Models;

namespace LibraryMVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenresApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GenresApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/GenresApi
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<Genre>>> GetGenres([FromQuery] int skip = 0, [FromQuery] int limit = 5)
        {
            var totalCount = await _context.Genres.CountAsync();

            var genres = await _context.Genres
                .OrderBy(g => g.Name)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            string? nextLink = null;
            if (skip + limit < totalCount)
            {
                nextLink = Url.Action(nameof(GetGenres), null, new { skip = skip + limit, limit = limit }, Request.Scheme);
            }

            var response = new PaginatedResponse<Genre>
            {
                Items = genres,
                TotalCount = totalCount,
                NextLink = nextLink
            };

            return Ok(response);
        }

        // POST: api/GenresApi
        [HttpPost]
        public async Task<ActionResult<Genre>> PostGenre(GenreDto genreDto)
        {
            var newGenre = new Genre
            {
                Name = genreDto.Name
            };

            _context.Genres.Add(newGenre);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGenre", new { id = newGenre.Id }, newGenre);
        }

        // PUT: api/GenresApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGenre(int id, GenreDto genreDto)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                return NotFound();
            }

            genre.Name = genreDto.Name;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/GenresApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                return NotFound();
            }

            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Genre>> GetGenre(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null) return NotFound();
            return genre;
        }
    }
}
