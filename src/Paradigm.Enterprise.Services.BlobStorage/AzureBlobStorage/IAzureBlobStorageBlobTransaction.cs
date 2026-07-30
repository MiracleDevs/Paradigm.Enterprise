namespace Paradigm.Enterprise.Services.BlobStorage.AzureBlobStorage;

/// <summary>
/// Provides download and overwrite operations for a single blob.
/// </summary>
/// <remarks>
/// A container may associate this wrapper with a lease for an existing blob. Disposing the wrapper
/// breaks that lease. The wrapper's download and upload requests do not attach the lease identifier.
/// </remarks>
public interface IAzureBlobStorageBlobTransaction : IAsyncDisposable
{
    /// <summary>
    /// Downloads the blob asynchronously.
    /// </summary>
    /// <returns>A readable response stream that the caller must dispose.</returns>
    Task<Stream> DownloadAsync();

    /// <summary>
    /// Uploads the content asynchronously.
    /// </summary>
    /// <param name="content">The content.</param>
    Task UploadAsync(string content);

    /// <summary>
    /// Checks if the file Exists.
    /// </summary>
    /// <returns><see langword="true"/> when the blob exists.</returns>
    Task<bool> ExistsAsync();
}
