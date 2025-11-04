using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryMVC.Data;
using LibraryMVC.Models;
using LibraryMVC.Services;
using Azure.Search.Documents; 
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Authorization;
using LibraryMVC.FileService;
using Microsoft.Extensions.Caching.Memory;


namespace LibraryMVC.Controllers
{
    public class BooksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly BlobStorageService _blobService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TelegramNotificationService _telegramService;
        private readonly SearchClient _searchClient;
        private readonly IMemoryCache _cache;
        public BooksController(AppDbContext context, BlobStorageService blobService, UserManager<ApplicationUser> userManager, TelegramNotificationService telegramService, IConfiguration configuration, IMemoryCache memoryCache)
        {
            _context = context;
            _blobService = blobService;
            _userManager = userManager;
            _telegramService = telegramService;
            _cache = memoryCache;
            var searchServiceUrl = configuration.GetValue<string>("AzureAiSearch:ServiceUrl");
            var searchAdminKey = configuration.GetValue<string>("AzureAiSearch:AdminApiKey");
            string indexName = "books-index";

            if (!string.IsNullOrEmpty(searchServiceUrl) && !string.IsNullOrEmpty(searchAdminKey))
            {
                Uri serviceEndpoint = new Uri(searchServiceUrl);
                Azure.AzureKeyCredential credential = new Azure.AzureKeyCredential(searchAdminKey);
                _searchClient = new SearchClient(serviceEndpoint, indexName, credential);
            }
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            bool isPremiumOrAdmin = false;

            if (user != null)
            {
                isPremiumOrAdmin = await _userManager.IsInRoleAsync(user, "Admin") ||
                                   await _userManager.IsInRoleAsync(user, "PremiumUser");
            }

            
            string cacheKey = isPremiumOrAdmin ? "Books_Premium" : "Books_Regular";

           
            var books = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                entry.SlidingExpiration = TimeSpan.FromMinutes(1);

                
                var booksQuery = _context.Books
                    .Include(b => b.Author)
                    .Include(b => b.Genres)
                    .AsQueryable();

                
                if (!isPremiumOrAdmin)
                {
                    booksQuery = booksQuery.Where(b => !b.IsPremiumOnly);
                }

                return await booksQuery.ToListAsync();
               
            });

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
           
            if (id == null) return NotFound();
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null) return NotFound();
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                var favoriteBookIds = await _context.Entry(user).Collection(u => u.FavoriteBooks).Query().Select(b => b.Id).ToListAsync();
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
        public async Task<IActionResult> Create([Bind("Title,AuthorId,Description,IsPremiumOnly")] Book book, List<int> genreIds, IFormFile? photoFile)
        {
            if (ModelState.IsValid)
            {
                if (photoFile != null)
                {
                    book.PhotoPath = await _blobService.UploadFileAsync("images", photoFile);
                }

                if (genreIds != null && genreIds.Any())
                {
                    book.Genres = await _context.Genres.Where(g => genreIds.Contains(g.Id)).ToListAsync();
                }

                _context.Add(book);
                await _context.SaveChangesAsync(); 

               
                if (_searchClient != null)
                {
                    var authorName = await _context.Authors.Where(a => a.Id == book.AuthorId).Select(a => a.Name).FirstOrDefaultAsync();
                    var doc = new BookSearchModel
                    {
                        Id = book.Id.ToString(),
                        Title = book.Title,
                        Description = book.Description,
                        AuthorName = authorName ?? "Невідомий"
                    };
                    await _searchClient.UploadDocumentsAsync(new[] { doc });
                }

                
                _cache.Remove("Books_Premium");
                _cache.Remove("Books_Regular");

                try
                {
                    var author = await _context.Authors.FindAsync(book.AuthorId);
                    var message = $"📚 <b>Нова книга в бібліотеці!</b>\n\n<b>Назва:</b> {book.Title}\n<b>Автор:</b> {author?.Name}";
                    await _telegramService.SendMessageAsync(message);
                }
                catch { }

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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,AuthorId,PhotoPath,Description,IsPremiumOnly")] Book book, List<int> genreIds, IFormFile? photoFile)
        {
            if (id != book.Id) return NotFound();

            var bookToUpdate = await _context.Books.Include(b => b.Genres).FirstOrDefaultAsync(b => b.Id == id);
            if (bookToUpdate == null) return NotFound();

            if (ModelState.IsValid)
            {
                bookToUpdate.Title = book.Title;
                bookToUpdate.AuthorId = book.AuthorId;
                bookToUpdate.Description = book.Description;
                bookToUpdate.IsPremiumOnly = book.IsPremiumOnly;

                if (photoFile != null)
                {
                    if (!string.IsNullOrEmpty(bookToUpdate.PhotoPath))
                    {
                        await _blobService.DeleteFileAsync(bookToUpdate.PhotoPath);
                    }
                    bookToUpdate.PhotoPath = await _blobService.UploadFileAsync("images", photoFile);
                }
                else
                {
                    bookToUpdate.PhotoPath = book.PhotoPath;
                }

                var selectedGenres = await _context.Genres.Where(g => genreIds.Contains(g.Id)).ToListAsync();
                bookToUpdate.Genres = selectedGenres;

                await _context.SaveChangesAsync(); 

                
                if (_searchClient != null)
                {
                    var authorName = await _context.Authors.Where(a => a.Id == bookToUpdate.AuthorId).Select(a => a.Name).FirstOrDefaultAsync();
                    var doc = new BookSearchModel
                    {
                        Id = bookToUpdate.Id.ToString(),
                        Title = bookToUpdate.Title,
                        Description = bookToUpdate.Description,
                        AuthorName = authorName ?? "Невідомий"
                    };
                    await _searchClient.MergeOrUploadDocumentsAsync(new[] { doc });
                }

                
                _cache.Remove("Books_Premium");
                _cache.Remove("Books_Regular");

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
                if (!string.IsNullOrEmpty(book.PhotoPath))
                {
                    await _blobService.DeleteFileAsync(book.PhotoPath);
                }

                _context.Books.Remove(book);
                await _context.SaveChangesAsync(); 

                
                if (_searchClient != null)
                {
                    var docToDelete = new BookSearchModel { Id = id.ToString() };
                    await _searchClient.DeleteDocumentsAsync(new[] { docToDelete });
                }

                
                _cache.Remove("Books_Premium");
                _cache.Remove("Books_Regular");
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
