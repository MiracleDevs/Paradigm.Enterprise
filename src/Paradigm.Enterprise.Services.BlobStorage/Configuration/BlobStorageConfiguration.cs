namespace Paradigm.Enterprise.Services.BlobStorage.Configuration;

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
}
