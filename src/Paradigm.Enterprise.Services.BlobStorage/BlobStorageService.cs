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
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration.StorageConnection, nameof(configuration.StorageConnection));

        var options = GetBlobClientOptions(configuration);

        if (isConnectionString)
        {
            ConnectionString = configuration.StorageConnection;
            _serviceClient = new BlobServiceClient(configuration.StorageConnection, options);
        }
        else
        {
            StorageAccountUri = configuration.StorageConnection;
            _serviceClient = new BlobServiceClient(new Uri(configuration.StorageConnection), new DefaultAzureCredential(), options);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates the using managed identity.
    /// </summary>
    /// <param name="storageAccountUri">The storage account URI.</param>
    /// <returns></returns>
    [Obsolete("Use CreateUsingManagedIdentity(BlobStorageConfiguration configuration) instead.")]
    public static BlobStorageService CreateUsingManagedIdentity(string storageAccountUri)
    {
        return CreateUsingManagedIdentity(new BlobStorageConfiguration { StorageConnection = storageAccountUri });
    }

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
    /// Creates the using connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns></returns>
    [Obsolete("Use CreateUsingConnectionString(BlobStorageConfiguration configuration) instead.")]
    public static BlobStorageService CreateUsingConnectionString(string connectionString)
    {
        return CreateUsingConnectionString(new BlobStorageConfiguration { StorageConnection = connectionString });
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

    #region Private Methods

    /// <summary>
    /// Gets the BLOB client options.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    private BlobClientOptions GetBlobClientOptions(BlobStorageConfiguration configuration)
    {
        var options = new BlobClientOptions
        {
            Retry =
            {
                Mode = Azure.Core.RetryMode.Exponential,
                Delay = TimeSpan.FromMilliseconds(Math.Max(1, configuration.RetryInitialDelayMilliseconds ?? 100)),
            }
        };

        if (configuration.MaxRetryAttempts.HasValue)
            options.Retry.MaxRetries = Math.Max(0, configuration.MaxRetryAttempts.Value);

        if (configuration.RetryInitialDelayMilliseconds.HasValue)
            options.Retry.Delay = TimeSpan.FromMilliseconds(Math.Max(1, configuration.RetryInitialDelayMilliseconds.Value));

        if (configuration.RetryMaxDelayMilliseconds.HasValue)
            options.Retry.MaxDelay = TimeSpan.FromMilliseconds(Math.Max(1, configuration.RetryMaxDelayMilliseconds.Value));

        if (configuration.RetryTimeoutMilliseconds.HasValue)
            options.Retry.NetworkTimeout = TimeSpan.FromMilliseconds(Math.Max(1, configuration.RetryTimeoutMilliseconds.Value));

        return options;
    }

    #endregion
}