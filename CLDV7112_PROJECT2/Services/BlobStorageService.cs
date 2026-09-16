using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT2.Services
{
    /// <summary>
    /// Service managing direct connection to Azure Blob Storage ('product-images' container).
    /// Used for uploading and deleting product photos and media assets.
    /// </summary>
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "product-images";

        public BlobStorageService(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        // Helper method returning container client with public access enabled
        private async Task<BlobContainerClient> GetContainerClientAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            return containerClient;
        }

        // Uploads a stream file to Azure Blob Storage and returns the public URL
        public async Task<string> UploadBlobAsync(string blobName, Stream content)
        {
            var containerClient = await GetContainerClientAsync();
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, overwrite: true);
            return blobClient.Uri.ToString();
        }

        // Deletes a blob from Azure Blob Storage container
        public async Task DeleteBlobAsync(string blobName)
        {
            var containerClient = await GetContainerClientAsync();
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}
