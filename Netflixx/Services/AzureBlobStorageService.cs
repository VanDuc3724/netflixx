// Services/AzureBlobStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration; // Thêm using
using System.Threading.Tasks;          // Thêm using
using Microsoft.AspNetCore.Http;       // Thêm using
using System.IO;                       // Thêm using


namespace Netflixx.Services
{
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobContainerClient _containerClient; // Chỉ cần giữ lại cái này

        public AzureBlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            var containerName = configuration["AzureBlobStorage:ContainerName"];

            // Khởi tạo container client một lần duy nhất
            _containerClient = new BlobContainerClient(connectionString, containerName);

            // Đảm bảo container tồn tại ngay khi service được tạo
            _containerClient.CreateIfNotExists(PublicAccessType.Blob);
        }

        public async Task<string> UploadAsync(IFormFile file, string fileName)
        {
            // Lấy tham chiếu đến blob (file) bằng cách dùng _containerClient đã được khởi tạo
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);

            await using Stream stream = file.OpenReadStream();

            // Dùng phiên bản UploadAsync đơn giản và đáng tin cậy
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        public async Task DeleteAsync(string fileName)
        {
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}