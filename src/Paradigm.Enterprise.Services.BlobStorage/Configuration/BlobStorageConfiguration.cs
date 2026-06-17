namespace Paradigm.Enterprise.Services.BlobStorage.Configuration;

public class BlobStorageConfiguration
{
    public required string StorageConnection { get; set; }
    public int? MaxRetryAttempts { get; set; }
    public int? RetryInitialDelayMilliseconds { get; set; }
}
