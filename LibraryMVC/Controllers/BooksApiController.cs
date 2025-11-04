using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryMVC.Data;
using Microsoft.Extensions.Caching.Memory;
using LibraryMVC.Models;
using LibraryMVC.DTOs;
using Azure.Search.Documents; 
using Azure.Search.Documents.Models; 
using Microsoft.Extensions.Configuration; 

namespace LibraryMVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly SearchClient _searchClient; 

        
        public BooksApiController(AppDbContext context,
                                  IMemoryCache cache,
                                  IConfiguration configuration) 
        {
            _context = context;
            _cache = cache;

           
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

        // GET: api/BooksApi 
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<Book>>> GetBooks([FromQuery] int skip = 0, [FromQuery] int limit = 5)
        {
           
            var totalCount = await _cache.GetOrCreateAsync("Api_Books_TotalCount", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _context.Books.CountAsync();
            });

            var books = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Genres)
                .OrderBy(b => b.Title)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            string? nextLink = null;
            if (skip + limit < totalCount)
            {
                nextLink = Url.Action(nameof(GetBooks), null, new { skip = skip + limit, limit = limit }, Request.Scheme);
            }

            var response = new PaginatedResponse<Book>
            {
                Items = books,
                TotalCount = totalCount,
                NextLink = nextLink
            };
            return Ok(response);
        }

        // GET: api/BooksApi/5 
        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            // (Твій код B9 - правильний)
            string cacheKey = $"Api_GetBook_{id}";
            var book = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _context.Books
                    .Include(b => b.Author)
                    .Include(b => b.Genres)
                    .FirstOrDefaultAsync(b => b.Id == id);
            });

            if (book == null) return NotFound();
            return book;
        }

        // POST: api/BooksApi 
        [HttpPost]
        public async Task<ActionResult<Book>> PostBook(BookDto bookDto)
        {
            var author = await _context.Authors.FindAsync(bookDto.AuthorId);
            if (author == null) return BadRequest("Author not found.");
            var genres = await _context.Genres.Where(g => bookDto.GenreIds.Contains(g.Id)).ToListAsync();
            var newBook = new Book
            {
                Title = bookDto.Title,
                AuthorId = bookDto.AuthorId,
                Author = author,
                Genres = genres,
                Description = bookDto.Description,
                TenantId = bookDto.TenantId
            };

            _context.Books.Add(newBook);
            await _context.SaveChangesAsync(); 

           
            if (_searchClient != null)
            {
                var doc = new BookSearchModel
                {
                    Id = newBook.Id.ToString(),
                    Title = newBook.Title,
                    Description = newBook.Description,
                    AuthorName = author.Name
                };
                await _searchClient.UploadDocumentsAsync(new[] { doc });
            }

            
            _cache.Remove("Api_Books_TotalCount");

            return CreatedAtAction("GetBook", new { id = newBook.Id }, newBook);
        }

        // PUT: api/BooksApi/5 
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, BookDto bookDto)
        {
            var bookToUpdate = await _context.Books.Include(b => b.Genres).FirstOrDefaultAsync(b => b.Id == id);
            if (bookToUpdate == null) return NotFound();

            var author = await _context.Authors.FindAsync(bookDto.AuthorId);
            if (author == null) return BadRequest("Author not found.");
            var genres = await _context.Genres.Where(g => bookDto.GenreIds.Contains(g.Id)).ToListAsync();
            if (genres.Count != bookDto.GenreIds.Count) return BadRequest("One or more genres not found.");

            bookToUpdate.Title = bookDto.Title;
            bookToUpdate.AuthorId = bookDto.AuthorId;
            bookToUpdate.Author = author;
            bookToUpdate.Genres = genres;
            bookToUpdate.Description = bookDto.Description;

            await _context.SaveChangesAsync(); 

           
            if (_searchClient != null)
            {
                var doc = new BookSearchModel
                {
                    Id = bookToUpdate.Id.ToString(),
                    Title = bookToUpdate.Title,
                    Description = bookToUpdate.Description,
                    AuthorName = author.Name
                };
                await _searchClient.MergeOrUploadDocumentsAsync(new[] { doc });
            }

          
            string cacheKey = $"Api_GetBook_{id}";
            _cache.Remove(cacheKey);

            return NoContent();
        }

        // DELETE: api/BooksApi/5 
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            
            if (_searchClient != null)
            {
                var docToDelete = new BookSearchModel { Id = id.ToString() };
                await _searchClient.DeleteDocumentsAsync(new[] { docToDelete });
            }

          
            string cacheKey = $"Api_GetBook_{id}";
            _cache.Remove(cacheKey);
            _cache.Remove("Api_Books_TotalCount");

            return NoContent();
        }
    }
}
