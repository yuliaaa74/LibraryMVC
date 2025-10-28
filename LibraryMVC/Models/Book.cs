    // Models/Book.cs
    using System.ComponentModel.DataAnnotations;

    namespace LibraryMVC.Models
    {
        public class Book
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        [Required]
        [Display(Name = "Назва")]
        public string Title { get; set; }

        [Display(Name = "Автор")]
        public int AuthorId { get; set; }
        public Author? Author { get; set; }

        [Display(Name = "Жанри")]
        public ICollection<Genre> Genres { get; set; } = new List<Genre>();

        [Display(Name = "Обкладинка")]
        public string? PhotoPath { get; set; }
            public string? Description { get; set; }
        public ICollection<ApplicationUser>? Users { get; set; }
        
    }
    }
