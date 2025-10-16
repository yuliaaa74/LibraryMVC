namespace LibraryMVC.DTOs
{
    public class BookDto
    {
        public string Title { get; set; }
        public int AuthorId { get; set; }
        public List<int> GenreIds { get; set; }
        public string? Description { get; set; }
    }
}
