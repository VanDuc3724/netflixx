// Services/IFileStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public interface IFileStorageService
{
    Task<string> UploadAsync(IFormFile file, string fileName);
    Task DeleteAsync(string fileName);
}

// Services/AzureBlobStorageService.cs
