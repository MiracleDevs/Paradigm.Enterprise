using Azure.Storage.Blobs.Models;
using Paradigm.Enterprise.Services.BlobStorage.AzureBlobStorage;
using Paradigm.Enterprise.Services.Core;

namespace Paradigm.Enterprise.Services.BlobStorage;

/// <summary>
/// Creates Azure Blob Storage container abstractions using configured credentials.
/// </summary>
public interface IBlobStorageService : IService
{
    /// <summary>
    /// Gets the connection string.
    /// </summary>
    /// <value>
    /// The connection string.
    /// </value>
    string? ConnectionString { get; }

    /// <summary>
    /// Gets the storage account URI.
    /// </summary>
    /// <value>
    /// The storage account URI.
    /// </value>
    string? StorageAccountUri { get; }

    /// <summary>
    /// Creates the BLOB storage container.
    /// </summary>
    /// <param name="containerName">Name of the container.</param>
    /// <param name="metadata">Extra metadata for the container.</param>
    /// <returns>A wrapper for the newly created container.</returns>
    Task<IAzureBlobStorageContainer> CreateBlobStorageContainerAsync(string containerName, IDictionary<string, string>? metadata = null);

    /// <summary>
    /// Gets the BLOB storage container.
    /// </summary>
    /// <param name="containerName">Name of the container.</param>
    /// <returns>A wrapper for the named container; the container is not created by this call.</returns>
    IAzureBlobStorageContainer GetBlobStorageContainer(string containerName);

    /// <summary>
    /// Gets all containers.
    /// </summary>
    /// <returns>All containers visible to the configured storage account, including metadata.</returns>
    Task<List<BlobContainerItem>> GetAllContainersAsync();
}
