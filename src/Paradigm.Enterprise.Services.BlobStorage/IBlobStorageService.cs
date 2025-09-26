using Azure.Storage.Blobs.Models;
using Paradigm.Enterprise.Services.BlobStorage.AzureBlobStorage;
using Paradigm.Enterprise.Services.Core;

namespace Paradigm.Enterprise.Services.BlobStorage;

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
    /// <returns></returns>
    Task<IAzureBlobStorageContainer> CreateBlobStorageContainerAsync(string containerName, IDictionary<string, string>? metadata = null);

    /// <summary>
    /// Gets the BLOB storage container.
    /// </summary>
    /// <param name="containerName">Name of the container.</param>
    /// <returns></returns>
    IAzureBlobStorageContainer GetBlobStorageContainer(string containerName);

    /// <summary>
    /// Gets all containers.
    /// </summary>
    /// <returns></returns>
    Task<List<BlobContainerItem>> GetAllContainersAsync();
}