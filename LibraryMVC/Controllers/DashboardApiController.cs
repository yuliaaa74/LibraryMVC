using LibraryMVC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryMVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardApiController(AppDbContext context)
        {
            _context = context;
        }

       
        [HttpGet("BooksPerGenre")]
        public async Task<IActionResult> GetBooksPerGenre()
        {
            var data = await _context.Genres
                .Include(g => g.Books)
                .Select(g => new {
                    GenreName = g.Name,
                    BookCount = g.Books.Count()
                })
                .Where(d => d.BookCount > 0)
                .OrderByDescending(d => d.BookCount)
                .ToListAsync();

            return Ok(data);
        }

    
        [HttpGet("MostFavoritedBooks")]
        public async Task<IActionResult> GetMostFavoritedBooks()
        {
            var data = await _context.Books
                .Include(b => b.Users)
                .Select(b => new {
                    BookTitle = b.Title,
                    FavoritesCount = b.Users.Count()
                })
                .Where(d => d.FavoritesCount > 0)
                .OrderByDescending(d => d.FavoritesCount)
                .Take(5) 
                .ToListAsync();

            return Ok(data);
        }
    }
}
