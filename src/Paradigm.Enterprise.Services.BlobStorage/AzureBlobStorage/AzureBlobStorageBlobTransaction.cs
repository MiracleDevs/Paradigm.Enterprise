using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Paradigm.Enterprise.Services.BlobStorage.Extensions;

namespace Paradigm.Enterprise.Services.BlobStorage.AzureBlobStorage;

public class AzureBlobStorageBlobTransaction : IAzureBlobStorageBlobTransaction
{
    #region Properties

    /// <summary>
    /// Gets the BLOB lease client.
    /// </summary>
    /// <value>
    /// The BLOB lease client.
    /// </value>
    private BlobLeaseClient? BlobLeaseClient { get; }

    /// <summary>
    /// Gets the BLOB client.
    /// </summary>
    /// <value>
    /// The BLOB client.
    /// </value>
    private BlobClient BlobClient { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="AzureBlobStorageBlobTransaction"/> is disposed.
    /// </summary>
    /// <value>
    ///   <c>true</c> if disposed; otherwise, <c>false</c>.
    /// </value>
    private bool Disposed { get; set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobStorageBlobTransaction" /> class.
    /// </summary>
    /// <param name="blobClient">The BLOB client.</param>
    /// <param name="blobLeaseClient">The BLOB lease client.</param>
    public AzureBlobStorageBlobTransaction(BlobClient blobClient, BlobLeaseClient? blobLeaseClient)
    {
        BlobClient = blobClient;
        BlobLeaseClient = blobLeaseClient;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Disposes the instance asynchronously.
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        if (BlobLeaseClient is not null && !Disposed)
        {
            await BlobLeaseClient.BreakAsync();
            Disposed = true;
        }
    }

    /// <summary>
    /// Downloads the blob asynchronously.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<Stream> DownloadAsync()
    {
        return (await BlobClient.DownloadAsync()).Value.Content;
    }

    /// <summary>
    /// Uploads the content asynchronously.
    /// </summary>
    /// <param name="content">The content.</param>
    public async Task UploadAsync(string content)
    {
        await using var stream = content.ToStream();
        await BlobClient.UploadAsync(stream, true);
    }

    /// <summary>
    /// Checks if the file Exists.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> ExistsAsync()
    {
        return await BlobClient.ExistsAsync();
    }

    #endregion
}