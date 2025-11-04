using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.ComponentModel.DataAnnotations;

namespace LibraryMVC.Models
{
    
    public class BookSearchModel
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; } 

        [SearchableField(IsSortable = true)]
        public string Title { get; set; }

        [SearchableField(IsFilterable = true, IsSortable = true)]
        public string AuthorName { get; set; }

        [SearchableField]
        public string Description { get; set; }
    }
}
