using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryMVC.Data;
using LibraryMVC.Models;
using LibraryMVC.Services;
using Microsoft.AspNetCore.Authorization;
using LibraryMVC.FileService;

namespace LibraryMVC.Controllers
{
    public class AuthorsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly BlobStorageService _blobService;

        public AuthorsController(AppDbContext context, BlobStorageService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Authors.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var author = await _context.Authors.FirstOrDefaultAsync(m => m.Id == id);
            if (author == null) return NotFound();
            return View(author);
        }

        [Authorize]
        public async Task<IActionResult> LandingPage(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (author == null)
            {
                return NotFound();
            }

            return View(author);
        }
        [Authorize(Roles = "Admin")] 
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Name,Biography,Birthplace,Latitude,Longitude")] Author author, IFormFile? photoFile)
        {
            if (ModelState.IsValid)
            {
                if (photoFile != null)
                {
                    string photoUrl = await _blobService.UploadFileAsync("images", photoFile);
                    author.PhotoPath = photoUrl;
                }

                _context.Add(author);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(author);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var author = await _context.Authors.FindAsync(id);
            if (author == null) return NotFound();
            return View(author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
       
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,PhotoPath,Biography,Birthplace,Latitude,Longitude")] Author author, IFormFile? photoFile)
        {
            if (id != author.Id) return NotFound();

            var authorToUpdate = await _context.Authors.FindAsync(id);
            if (authorToUpdate == null) return NotFound();

            if (ModelState.IsValid)
            {
                authorToUpdate.Name = author.Name;
                
                authorToUpdate.Biography = author.Biography;
                authorToUpdate.Birthplace = author.Birthplace;
                authorToUpdate.Latitude = author.Latitude;
                authorToUpdate.Longitude = author.Longitude;
                if (photoFile != null)
                {
                    if (!string.IsNullOrEmpty(author.PhotoPath))
                    {
                        await _blobService.DeleteFileAsync(author.PhotoPath);
                    }

                    string newPhotoUrl = await _blobService.UploadFileAsync("images", photoFile);
                    authorToUpdate.PhotoPath = newPhotoUrl; // <-- ГОЛОВНИЙ ФІКС
                }
                else
                {
                    // Якщо файл не завантажено, нам все одно 
                    // треба оновити PhotoPath на випадок, 
                    // якщо його видалили, але не замінили.
                    // Вхідна модель `author` має містити старий шлях.
                    authorToUpdate.PhotoPath = author.PhotoPath;
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

           
            return View(authorToUpdate);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var author = await _context.Authors.FirstOrDefaultAsync(m => m.Id == id);
            if (author == null) return NotFound();
            return View(author);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author != null)
            {

                await _blobService.DeleteFileAsync(author.PhotoPath);
                _context.Authors.Remove(author);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AuthorExists(int id)
        {
            return _context.Authors.Any(e => e.Id == id);
        }
    }
}
