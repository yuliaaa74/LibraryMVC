using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using LibraryMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace LibraryMVC.Controllers
{
    [Route("api/search")] 
    [ApiController]
    public class SearchApiController : ControllerBase
    {
        private readonly SearchClient _searchClient;

        public SearchApiController(IConfiguration configuration)
        {
           
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

        // GET: api/search?query=текст
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (_searchClient == null)
            {
                return StatusCode(503, "Сервіс пошуку не налаштований.");
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new string[0]); 
            }

            try
            {
                
                SearchResults<BookSearchModel> results = await _searchClient.SearchAsync<BookSearchModel>(query);

                
                return Ok(results.GetResults());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Помилка пошуку: {ex.Message}");
            }
        }
    }
}
