namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Configures blob storage for an Aspire application.
/// </summary>
public sealed class ParadigmBlobStorageOptions
{
    #region Properties

    /// <summary>
    /// Gets or sets whether storage is managed by Aspire or supplied externally.
    /// </summary>
    public ParadigmResourceMode Mode { get; init; } = ParadigmResourceMode.Managed;

    /// <summary>
    /// Gets or sets the Azure Storage resource name.
    /// </summary>
    public string StorageName { get; init; } = "storage";

    /// <summary>
    /// Gets or sets the blobs resource name.
    /// </summary>
    public string BlobResourceName { get; init; } = "blobs";

    /// <summary>
    /// Gets or sets the connection-string name injected into consumers.
    /// </summary>
    public string ConnectionName { get; init; } = "BlobStorageConnection";

    /// <summary>
    /// Gets or sets the optional persistent Azurite volume name.
    /// </summary>
    public string? DataVolumeName { get; init; }

    /// <summary>
    /// Gets or sets the optional fixed blob endpoint port.
    /// </summary>
    public int? BlobPort { get; init; }

    #endregion
}
