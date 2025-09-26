using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Paradigm.Enterprise.Services.BlobStorage.AzureBlobStorage;

namespace Paradigm.Enterprise.Services.BlobStorage;

public class BlobStorageService : IBlobStorageService
{
    #region Properties

    /// <summary>
    /// The service client
    /// </summary>
    private readonly BlobServiceClient _serviceClient;

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    /// <value>
    /// The connection string.
    /// </value>
    public string? ConnectionString { get; }

    /// <summary>
    /// Gets the storage account URI.
    /// </summary>
    /// <value>
    /// The storage account URI.
    /// </value>
    public string? StorageAccountUri { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageService" /> class.
    /// </summary>
    /// <param name="storageConnection">The storage connection.</param>
    /// <param name="isConnectionString">if set to <c>true</c> [is connection string].</param>
    private BlobStorageService(string storageConnection, bool isConnectionString)
    {
        if (isConnectionString)
        {
            ConnectionString = storageConnection;
            _serviceClient = new BlobServiceClient(storageConnection);
        }
        else
        {
            StorageAccountUri = storageConnection;
            _serviceClient = new BlobServiceClient(new Uri(storageConnection), new DefaultAzureCredential());
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates the service using managed identity.
    /// </summary>
    /// <param name="storageAccountUri">The storage account URI.</param>
    /// <returns></returns>
    public static BlobStorageService CreateUsingManagedIdentity(string storageAccountUri)
    {
        return new BlobStorageService(storageAccountUri, false);
    }

    /// <summary>
    /// Creates the service using connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns></returns>
    public static BlobStorageService CreateUsingConnectionString(string connectionString)
    {
        return new BlobStorageService(connectionString, true);
    }

    /// <summary>
    /// Creates the BLOB storage container.
    /// </summary>
    /// <param name="containerName">Name of the container.</param>
    /// <param name="metadata">Extra metadata properties to tag the container.</param>
    /// <returns></returns>
    /// <exception cref="Exception">Unable to create the container.</exception>
    public async Task<IAzureBlobStorageContainer> CreateBlobStorageContainerAsync(string containerName, IDictionary<string, string>? metadata = null)
    {
        var containerClient = ((await _serviceClient.CreateBlobContainerAsync(containerName))?.Value) ?? throw new Exception("Unable to create the container.");
        if (metadata is not null) await containerClient.SetMetadataAsync(metadata);
        return new AzureBlobStorageContainer(containerClient);
    }

    /// <summary>
    /// Gets the BLOB storage container.
    /// </summary>
    /// <param name="containerName">Name of the container.</param>
    /// <returns></returns>
    public IAzureBlobStorageContainer GetBlobStorageContainer(string containerName)
    {
        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        return new AzureBlobStorageContainer(containerClient);
    }

    /// <summary>
    /// Gets all containers.
    /// </summary>
    /// <returns></returns>
    public async Task<List<BlobContainerItem>> GetAllContainersAsync()
    {
        var containers = new List<BlobContainerItem>();

        await foreach (var containerClientPage in _serviceClient.GetBlobContainersAsync(BlobContainerTraits.Metadata).AsPages(default, 100))
            containers.AddRange(containerClientPage.Values);

        return containers;
    }

    #endregion
}