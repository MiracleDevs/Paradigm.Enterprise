using Azure.Storage.Blobs;

namespace Paradigm.Enterprise.Services.BlobStorage.AzureBlobStorage;

public interface IAzureBlobStorageContainer
{
    /// <summary>
    /// Creates the transaction for a file asynchronously.
    /// </summary>
    /// <param name="blobName">Name of the BLOB.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<IAzureBlobStorageBlobTransaction> CreateTransactionForFileAsync(string blobName, CancellationToken cancellationToken);

    /// <summary>
    /// Uploads the Cloud Storage Blob.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="fileStream">The file stream.</param>
    /// <param name="contentType">The file content type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="blobName">An optional blob name.</param>
    /// <returns></returns>
    Task<Uri> UploadFileAsync(string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken, string? blobName = null);

    /// <summary>
    /// Uploads the Cloud Storage Blob.
    /// </summary>
    /// <param name="content">The json content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="blobName">An optional blob name.</param>
    /// <returns></returns>
    Task<Uri> UploadJsonAsync(string content, CancellationToken cancellationToken, string? blobName = null);

    /// <summary>
    /// Deletes the Cloud Storage Blob.
    /// </summary>
    /// <param name="blobName">Name of the BLOB.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task DeleteBlobAsync(string blobName, CancellationToken cancellationToken);

    /// <summary>
    /// Copies the folder.
    /// </summary>
    /// <param name="from">From.</param>
    /// <param name="to">To.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task CopyFolderAsync(string from, string to, CancellationToken cancellationToken);

    /// <summary>
    /// Copies the folder between containers asynchronous.
    /// </summary>
    /// <param name="from">From.</param>
    /// <param name="to">To.</param>
    /// <param name="destinationContainer">The destination container.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task CopyFolderBetweenContainersAsync(string from, string to, IAzureBlobStorageContainer destinationContainer, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the BLOB exists.
    /// </summary>
    /// <param name="blobName">Name of the file.</param>
    /// <returns></returns>
    Task<bool> BlobExistsAsync(string blobName);

    /// <summary>
    /// Checks if the BLOB exists.
    /// </summary>
    /// <param name="blobUri">The BLOB URI.</param>
    /// <returns></returns>
    Task<bool> BlobExistsAsync(Uri blobUri);

    /// <summary>
    /// Downloads the blob as stream.
    /// </summary>
    /// <param name="blobName">Name of the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the blob as stream.
    /// </summary>
    /// <param name="blobUri">The BLOB URI.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<Stream> DownloadAsync(Uri blobUri, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the metadata.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task UpdateMetadataAsync(Dictionary<string, string> metadata, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <param name="blobName">Name of the BLOB.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<Dictionary<string, object>> GetPropertiesAsync(string blobName, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <param name="blobUri">The BLOB URI.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<Dictionary<string, object>> GetPropertiesAsync(Uri blobUri, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the container exists.
    /// </summary>
    /// <returns></returns>
    Task<bool> ExistsAsync();

    /// <summary>
    /// Deletes the container.
    /// </summary>
    /// <returns></returns>
    Task DeleteAsync();

    /// <summary>
    /// Copies the container.
    /// </summary>
    /// <param name="destinationContainer">The destination container.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task CopyAsync(IAzureBlobStorageContainer destinationContainer, CancellationToken cancellationToken);

    /// <summary>
    /// Return a blob client from the container
    /// </summary>
    /// <param name="blobName"></param>
    /// <returns></returns>
    BlobClient GetBlobClient(string blobName);

    /// <summary>
    /// Return a blob client from the provided URI
    /// </summary>
    /// <param name="blobUri">The BLOB URI.</param>
    /// <returns></returns>
    BlobClient GetBlobClient(Uri blobUri);
}