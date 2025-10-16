using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace LibraryMVC.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public ICollection<Book>? FavoriteBooks { get; set; }
    }
}
