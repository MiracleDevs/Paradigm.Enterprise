using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Paradigm.Enterprise.Services.BlobStorage.AzureBlobStorage;
using Paradigm.Enterprise.Services.BlobStorage.Configuration;

namespace Paradigm.Enterprise.Services.BlobStorage;

public class BlobStorageService : IBlobStorageService
{
    #region Properties

    /// <summary>
    /// The service client
    /// </summary>
    private readonly BlobServiceClient _serviceClient;

    /// <summary>
    /// The configuration
    /// </summary>
    private readonly BlobStorageConfiguration _configuration;

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
    /// <param name="configuration">The configuration.</param>
    /// <param name="isConnectionString">if set to <c>true</c> [is connection string].</param>
    private BlobStorageService(BlobStorageConfiguration configuration, bool isConnectionString)
    {
        if (isConnectionString)
        {
            ConnectionString = configuration.StorageConnection;
            _serviceClient = new BlobServiceClient(configuration.StorageConnection);
        }
        else
        {
            StorageAccountUri = configuration.StorageConnection;
            _serviceClient = new BlobServiceClient(new Uri(configuration.StorageConnection), new DefaultAzureCredential());
        }

        _configuration = configuration;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates the service using managed identity.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    public static BlobStorageService CreateUsingManagedIdentity(BlobStorageConfiguration configuration)
    {
        return new BlobStorageService(configuration, false);
    }

    /// <summary>
    /// Creates the service using connection string.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    public static BlobStorageService CreateUsingConnectionString(BlobStorageConfiguration configuration)
    {
        return new BlobStorageService(configuration, true);
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
        return new AzureBlobStorageContainer(containerClient, _configuration);
    }

    /// <summary>
    /// Gets the BLOB storage container.
    /// </summary>
    /// <param name="containerName">Name of the container.</param>
    /// <returns></returns>
    public IAzureBlobStorageContainer GetBlobStorageContainer(string containerName)
    {
        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        return new AzureBlobStorageContainer(containerClient, _configuration);
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