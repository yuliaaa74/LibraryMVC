using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryMVC.Models
{
    public class Genre
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        [Required] public string Name { get; set; }
        public ICollection<Book>? Books { get; set; }
        
    }
}
