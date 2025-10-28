using System.Collections.Generic;

namespace LibraryMVC.DTOs
{
   
    public class PaginatedResponse<T>
    {
        
        public List<T> Items { get; set; } = new List<T>();

       
        public int TotalCount { get; set; }

        
        public string? NextLink { get; set; }
    }
}
