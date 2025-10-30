using LibraryMVC.Data;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryMVC.Controllers
{
    [Authorize(Roles = "Admin, PremiumUser")]
    public class PremiumController : Controller
    {
        
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PremiumController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Premium Контент";

            
            var premiumBooks = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Genres)
                .Where(b => b.IsPremiumOnly)
                .ToListAsync();

            return View(premiumBooks);
        }
    }
}
