using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;

namespace LibraryMVC.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Tenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

       
            builder.Entity<Book>().HasQueryFilter(b => GetTenantId() == -1 || b.TenantId == GetTenantId());
            builder.Entity<Author>().HasQueryFilter(a => GetTenantId() == -1 || a.TenantId == GetTenantId());
            builder.Entity<Genre>().HasQueryFilter(g => GetTenantId() == -1 || g.TenantId == GetTenantId());

        }

        private int GetTenantId()
        {
            
            var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrEmpty(tenantClaim) && int.TryParse(tenantClaim, out int tenantId))
            {
                return tenantId;
            }

            
            return -1;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetTenantIdForNewEntities();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void SetTenantIdForNewEntities()
        {
            var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            if (tenantClaim == null) return;

            if (int.TryParse(tenantClaim, out int tenantId))
            {
                var entries = ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added && (e.Entity is Book || e.Entity is Author || e.Entity is Genre));

                foreach (var entry in entries)
                {
                    if (entry.Entity is Book book) book.TenantId = tenantId;
                    if (entry.Entity is Author author) author.TenantId = tenantId;
                    if (entry.Entity is Genre genre) genre.TenantId = tenantId;
                }
            }
        }
    }
}
