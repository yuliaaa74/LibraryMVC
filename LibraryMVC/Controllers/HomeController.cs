using LibraryMVC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        
        public HomeController(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Statistics()
        {
            return View();
        }

       
        public async Task<IActionResult> Map()
        {
            
            ViewBag.MapboxToken = _configuration["Mapbox:Token"];

            
            ViewBag.Genres = await _context.Genres
                .Select(g => new { g.Id, g.Name })
                .ToListAsync();

            return View();
        }
        [Authorize] 
        [Route("ReadingList")]
        public IActionResult ReadingList()
        {
            return View();
        }
    }
}
