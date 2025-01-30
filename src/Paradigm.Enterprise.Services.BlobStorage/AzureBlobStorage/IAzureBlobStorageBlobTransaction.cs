namespace Paradigm.Enterprise.Services.BlobStorage.AzureBlobStorage;

public interface IAzureBlobStorageBlobTransaction : IAsyncDisposable
{
    /// <summary>
    /// Downloads the blob asynchronously.
    /// </summary>
    Task<Stream> DownloadAsync();

    /// <summary>
    /// Uploads the content asynchronously.
    /// </summary>
    /// <param name="content">The content.</param>
    Task UploadAsync(string content);

    /// <summary>
    /// Checks if the file Exists.
    /// </summary>
    Task<bool> ExistsAsync();
}