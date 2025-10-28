using LibraryMVC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryMVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MapApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("locations")]
        public async Task<IActionResult> GetAuthorLocations([FromQuery] int? genreId)
        {
            var query = _context.Authors
                .Where(a => a.Latitude != null && a.Longitude != null);

            
            if (genreId.HasValue)
            {
                query = query.Where(a => a.Books.Any(b => b.Genres.Any(g => g.Id == genreId.Value)));
            }

            var locations = await query
                .Select(a => new
                {
                    a.Name,
                    a.Birthplace,
                    a.Latitude,
                    a.Longitude,
                    a.PhotoPath,
                    Url = Url.Action("LandingPage", "Authors", new { id = a.Id }, Request.Scheme)
                })
                .ToListAsync();

            return Ok(locations);
        }
    }
}
