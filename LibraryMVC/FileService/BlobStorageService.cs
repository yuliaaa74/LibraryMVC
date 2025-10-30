using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace LibraryMVC.FileService
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageService(IConfiguration configuration)
        {
            _blobServiceClient = new BlobServiceClient(configuration["AzureStorage:ConnectionString"]);
        }

        public async Task<string> UploadFileAsync(string containerName, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return null; 
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
           
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

           
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(fileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            
            return blobClient.Uri.ToString();
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            
            Uri uri = new Uri(fileUrl);
            string containerName = uri.Segments[1].TrimEnd('/'); 
            string fileName = uri.Segments.Last(); 

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            try
            {
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Помилка видалення файлу {fileUrl}: {ex.Message}");
            }
        }
    }
}
