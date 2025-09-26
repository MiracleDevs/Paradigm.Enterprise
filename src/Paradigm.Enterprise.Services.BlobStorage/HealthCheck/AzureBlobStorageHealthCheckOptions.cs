namespace Paradigm.Enterprise.Services.BlobStorage.HealthCheck;
internal class AzureBlobStorageHealthCheckOptions
{
    /// <summary>
    /// Gets the connection string.
    /// </summary>
    /// <value>
    /// The connection string.
    /// </value>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the storage account URI for managed identity authentication.
    /// </summary>
    /// <value>
    /// The storage account URI (e.g., https://mystorageaccount.blob.core.windows.net/).
    /// </value>
    public string? StorageAccountUri { get; set; }

    /// <summary>
    /// Gets the name of the container.
    /// </summary>
    /// <value>
    /// The name of the container.
    /// </value>
    public string? ContainerName { get; set; }
}
