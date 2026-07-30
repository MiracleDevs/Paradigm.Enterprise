namespace Paradigm.Enterprise.Services.BlobStorage.Configuration;

/// <summary>
/// Configures Azure Blob Storage credentials and retry behavior.
/// </summary>
public class BlobStorageConfiguration
{
    /// <summary>
    /// Gets or sets the storage connection.
    /// </summary>
    /// <value>
    /// The storage connection.
    /// </value>
    public string StorageConnection { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum retry attempts.
    /// </summary>
    /// <value>
    /// The maximum retry attempts.
    /// </value>
    public int? MaxRetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the retry initial delay milliseconds.
    /// </summary>
    /// <value>
    /// The retry initial delay milliseconds.
    /// </value>
    public int? RetryInitialDelayMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the retry maximum delay milliseconds.
    /// </summary>
    /// <value>
    /// The retry maximum delay milliseconds.
    /// </value>
    public int? RetryMaxDelayMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the retry timeout milliseconds.
    /// </summary>
    /// <value>
    /// The retry timeout milliseconds.
    /// </value>
    public int? RetryTimeoutMilliseconds { get; set; }
}
