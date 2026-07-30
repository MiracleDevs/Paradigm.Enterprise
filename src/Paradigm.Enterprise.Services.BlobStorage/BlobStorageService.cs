using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Paradigm.Enterprise.Services.BlobStorage.AzureBlobStorage;
using Paradigm.Enterprise.Services.BlobStorage.Configuration;

namespace Paradigm.Enterprise.Services.BlobStorage;

/// <summary>
/// Connects to an Azure Blob Storage account using a connection string or managed identity.
/// </summary>
/// <remarks>
/// Prefer the <see cref="BlobStorageConfiguration"/> factory overloads so retry settings are
/// applied consistently. Managed identity uses <see cref="DefaultAzureCredential"/>; connection
/// string authentication stores the supplied connection string in <see cref="ConnectionString"/>.
/// Container wrappers do not require disposal.
/// </remarks>
/// <example>
/// Upload a caller-owned stream with managed identity:
/// <code>
/// var storage = BlobStorageService.CreateUsingManagedIdentity(
///     new BlobStorageConfiguration
///     {
///         StorageConnection = "https://contoso.blob.core.windows.net"
///     });
///
/// var container = storage.GetBlobStorageContainer("exports");
/// await using var content = File.OpenRead("daily.csv");
/// var blobUri = await container.UploadFileAsync(
///     "daily.csv",
///     content,
///     "text/csv",
///     cancellationToken);
/// </code>
/// <see cref="AzureBlobStorage.IAzureBlobStorageContainer.UploadFileAsync"/> leaves
/// <c>content</c> open; the caller disposes it.
/// </example>
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
    /// Creates a service using a storage account URI and managed identity.
    /// </summary>
    /// <param name="storageAccountUri">The storage account URI.</param>
    /// <returns>A configured blob storage service.</returns>
    [Obsolete("Use CreateUsingManagedIdentity(BlobStorageConfiguration configuration) instead.")]
    public static BlobStorageService CreateUsingManagedIdentity(string storageAccountUri)
    {
        return CreateUsingManagedIdentity(new BlobStorageConfiguration { StorageConnection = storageAccountUri });
    }

    /// <summary>
    /// Creates the service using managed identity.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>A configured blob storage service.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><see cref="BlobStorageConfiguration.StorageConnection"/> is empty.</exception>
    /// <exception cref="UriFormatException"><see cref="BlobStorageConfiguration.StorageConnection"/> is not an absolute storage account URI.</exception>
    public static BlobStorageService CreateUsingManagedIdentity(BlobStorageConfiguration configuration)
    {
        return new BlobStorageService(configuration, false);
    }

    /// <summary>
    /// Creates a service using a storage account connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A configured blob storage service.</returns>
    [Obsolete("Use CreateUsingConnectionString(BlobStorageConfiguration configuration) instead.")]
    public static BlobStorageService CreateUsingConnectionString(string connectionString)
    {
        return CreateUsingConnectionString(new BlobStorageConfiguration { StorageConnection = connectionString });
    }

    /// <summary>
    /// Creates the service using connection string.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>A configured blob storage service.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><see cref="BlobStorageConfiguration.StorageConnection"/> is empty.</exception>
    public static BlobStorageService CreateUsingConnectionString(BlobStorageConfiguration configuration)
    {
        return new BlobStorageService(configuration, true);
    }

    /// <summary>
    /// Creates the BLOB storage container.
    /// </summary>
    /// <param name="containerName">Name of the container.</param>
    /// <param name="metadata">Extra metadata properties to tag the container.</param>
    /// <returns>A wrapper for the newly created container.</returns>
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
    /// <returns>A wrapper for the named container; the container is not created by this call.</returns>
    public IAzureBlobStorageContainer GetBlobStorageContainer(string containerName)
    {
        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        return new AzureBlobStorageContainer(containerClient);
    }

    /// <summary>
    /// Gets all containers.
    /// </summary>
    /// <returns>All containers visible to the account, including metadata.</returns>
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
    /// <returns>Azure SDK client options with normalized retry values.</returns>
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
