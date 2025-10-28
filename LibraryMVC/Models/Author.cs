using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryMVC.Models
{
    public class Author
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        [Required] public string Name { get; set; }
        public string? PhotoPath { get; set; } 
        public ICollection<Book>? Books { get; set; }
        public string? Biography { get; set; }
        public string? Birthplace { get; set; } 
        public double? Latitude { get; set; }  
        public double? Longitude { get; set; }

    }
}
