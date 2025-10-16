using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryMVC.Data;
using LibraryMVC.Models;
using LibraryMVC.Services;

using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Authorization;


namespace LibraryMVC.Controllers
{
    public class BooksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TelegramNotificationService _telegramService;

        public BooksController(AppDbContext context, FileService fileService, UserManager<ApplicationUser> userManager, TelegramNotificationService telegramService)
        {
            _context = context;
            _fileService = fileService;
            _userManager = userManager;
            _telegramService = telegramService;
        }

        public async Task<IActionResult> Index()
        {
            var books = await _context.Books.Include(b => b.Author).Include(b => b.Genres).ToListAsync();
            return View(books);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var book = await _context.Books.Include(b => b.Author).Include(b => b.Genres).FirstOrDefaultAsync(m => m.Id == id);
            if (book == null) return NotFound();
            return View(book);
        }
        [Authorize]
        public async Task<IActionResult> LandingPage(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
            {
                return NotFound();
            }

           
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                
                var favoriteBookIds = await _context.Entry(user)
                    .Collection(u => u.FavoriteBooks)
                    .Query()
                    .Select(b => b.Id)
                    .ToListAsync();

               
                ViewBag.IsFavorite = favoriteBookIds.Contains(book.Id);
            }
            else
            {
                ViewBag.IsFavorite = false; 
            }
          

            return View(book);
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "Name");
            ViewData["AllGenres"] = new MultiSelectList(_context.Genres, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Title,AuthorId,Description")] Book book, List<int> genreIds, IFormFile? photoFile)
        {
            if (ModelState.IsValid)
            {
                if (photoFile != null)
                {
                    book.PhotoPath = await _fileService.SaveFileAsync(photoFile, "images/books");
                }

                if (genreIds != null && genreIds.Any())
                {
                    book.Genres = await _context.Genres.Where(g => genreIds.Contains(g.Id)).ToListAsync();
                }

                _context.Add(book);
                await _context.SaveChangesAsync();

                try
                {
                    
                    var author = await _context.Authors.FindAsync(book.AuthorId);
                    var message = $"📚 <b>Нова книга в бібліотеці!</b>\n\n<b>Назва:</b> {book.Title}\n<b>Автор:</b> {author?.Name}";
                    await _telegramService.SendMessageAsync(message);
                }
                catch
                {
                   
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "Name", book.AuthorId);
            ViewData["AllGenres"] = new MultiSelectList(_context.Genres, "Id", "Name", genreIds);
            return View(book);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var book = await _context.Books.Include(b => b.Genres).FirstOrDefaultAsync(b => b.Id == id);
            if (book == null) return NotFound();

            var genreIds = book.Genres?.Select(g => g.Id).ToList();

            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "Name", book.AuthorId);
            ViewData["AllGenres"] = new MultiSelectList(_context.Genres, "Id", "Name", genreIds);
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,AuthorId,PhotoPath,Description")] Book book, List<int> genreIds, IFormFile? photoFile)
        {
            if (id != book.Id) return NotFound();

            var bookToUpdate = await _context.Books.Include(b => b.Genres).FirstOrDefaultAsync(b => b.Id == id);
            if (bookToUpdate == null) return NotFound();

            if (ModelState.IsValid)
            {
                bookToUpdate.Title = book.Title;
                bookToUpdate.AuthorId = book.AuthorId;
                bookToUpdate.Description = book.Description;
                if (photoFile != null)
                {
                    _fileService.DeleteFile(bookToUpdate.PhotoPath);
                  
                    bookToUpdate.PhotoPath = await _fileService.SaveFileAsync(photoFile, "images/books");
                }

                var selectedGenres = await _context.Genres.Where(g => genreIds.Contains(g.Id)).ToListAsync();
                bookToUpdate.Genres = selectedGenres;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "Name", book.AuthorId);
            ViewData["AllGenres"] = new MultiSelectList(_context.Genres, "Id", "Name", genreIds);
            return View(bookToUpdate);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var book = await _context.Books.Include(b => b.Author).FirstOrDefaultAsync(m => m.Id == id);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
             
                _fileService.DeleteFile(book.PhotoPath);
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [Authorize] 
        public async Task<IActionResult> AddToFavorites(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); 

            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return NotFound();

            
            await _context.Entry(user).Collection(u => u.FavoriteBooks).LoadAsync();

            if (user.FavoriteBooks == null)
            {
                user.FavoriteBooks = new List<Book>();
            }

            
            if (!user.FavoriteBooks.Any(b => b.Id == bookId))
            {
                user.FavoriteBooks.Add(book);
                await _context.SaveChangesAsync();
            }

           
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveFromFavorites(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return NotFound();

            await _context.Entry(user).Collection(u => u.FavoriteBooks).LoadAsync();

            if (user.FavoriteBooks != null && user.FavoriteBooks.Any(b => b.Id == bookId))
            {
                user.FavoriteBooks.Remove(book);
                await _context.SaveChangesAsync();
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }
        
        [HttpGet]
        [Authorize] 
        public async Task<IActionResult> MyFavorites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            
            var userWithFavorites = await _context.Users
                .Where(u => u.Id == user.Id)
                .Include(u => u.FavoriteBooks)
                .ThenInclude(b => b.Author)
                .FirstOrDefaultAsync();

            var favoriteBooks = userWithFavorites?.FavoriteBooks ?? new List<Book>();

            ViewData["Title"] = "Мої обрані книги";
          
            return View("~/Views/Books/Index.cshtml", favoriteBooks);
        }
        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }
    }
}
