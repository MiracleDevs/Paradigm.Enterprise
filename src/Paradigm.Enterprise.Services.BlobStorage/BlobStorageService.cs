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
    /// The connection string
    /// </summary>
    private readonly string _connectionString;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageService" /> class.
    /// </summary>
    /// <param name="connectionString">The blob storage account connection string.</param>
    public BlobStorageService(string connectionString)
    {
        _connectionString = connectionString;
        _serviceClient = new BlobServiceClient(connectionString);
    }

    #endregion

    #region Public Methods

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
        var containerClient = new BlobContainerClient(_connectionString, containerName);
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

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    /// <returns></returns>
    public string GetConnectionString() => _connectionString;

    #endregion
}