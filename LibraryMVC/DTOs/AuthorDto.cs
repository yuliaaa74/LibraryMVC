namespace LibraryMVC.DTOs
{
    public class AuthorDto
    {
        public string Name { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
        public string? Biography { get; set; }
        public int TenantId { get; set; }
    }
}
