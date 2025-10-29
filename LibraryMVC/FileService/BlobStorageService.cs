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
                return null; // Або кидати виняток
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            // Переконуємось, що контейнер існує (можна зробити один раз при старті)
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Генеруємо унікальне ім'я файлу, щоб уникнути конфліктів
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(fileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            // Повертаємо ПУБЛІЧНУ URL-адресу завантаженого файлу
            return blobClient.Uri.ToString();
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            // Парсимо URL, щоб отримати ім'я контейнера та файлу
            Uri uri = new Uri(fileUrl);
            string containerName = uri.Segments[1].TrimEnd('/'); // Перший сегмент після хоста
            string fileName = uri.Segments.Last(); // Останній сегмент

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            try
            {
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                // Логування помилки, якщо потрібно
                Console.WriteLine($"Помилка видалення файлу {fileUrl}: {ex.Message}");
            }
        }
    }
}
