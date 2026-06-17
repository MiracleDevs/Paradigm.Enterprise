using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Paradigm.Enterprise.Services.BlobStorage.Configuration;
using Paradigm.Enterprise.Services.BlobStorage.Extensions;
using System.Web;

namespace Paradigm.Enterprise.Services.BlobStorage.AzureBlobStorage
{
    public class AzureBlobStorageContainer : IAzureBlobStorageContainer
    {
        #region Constants

        /// <summary>
        /// Maximum number of retry attempts for upload and download operations
        /// </summary>
        private readonly int _maxRetryAttempts;

        /// <summary>
        /// Initial delay in milliseconds for exponential backoff
        /// </summary>
        private readonly int _initialDelayMs;

        #endregion

        #region Properties

        /// <summary>
        /// The container client
        /// </summary>
        private readonly BlobContainerClient _containerClient;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobStorageContainer"/> class.
        /// </summary>
        /// <param name="containerClient">The container client.</param>
        /// <param name="configuration">The configuration.</param>
        public AzureBlobStorageContainer(BlobContainerClient containerClient, BlobStorageConfiguration configuration)
        {
            _containerClient = containerClient;
            _maxRetryAttempts = configuration.MaxRetryAttempts ?? 3;
            _initialDelayMs = configuration.RetryInitialDelayMilliseconds ?? 100;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates the transaction for a file asynchronously.
        /// </summary>
        /// <param name="blobName">Name of the BLOB.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<IAzureBlobStorageBlobTransaction> CreateTransactionForFileAsync(string blobName, CancellationToken cancellationToken)
        {
            var blob = GetBlobClient(blobName);

            if (!await blob.ExistsAsync())
                return new AzureBlobStorageBlobTransaction(blob, null);

            var lease = blob.GetBlobLeaseClient();
            await lease.AcquireAsync(TimeSpan.FromSeconds(-1), cancellationToken: cancellationToken);

            return new AzureBlobStorageBlobTransaction(blob, lease);
        }

        /// <summary>
        /// Uploads the Cloud Storage Blob.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="contentType">The file content type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="blobName">An optional blob name.</param>
        /// <returns></returns>
        public async Task<Uri> UploadFileAsync(string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken, string? blobName = null)
        {
            var newBlobName = blobName ?? $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var blob = GetBlobClient(newBlobName);

            await UploadWithRetryAsync(async () =>
            {
                await blob.UploadAsync(fileStream, true, cancellationToken);
                await blob.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
            }, cancellationToken);

            return blob.Uri;
        }

        /// <summary>
        /// Uploads the Cloud Storage Blob.
        /// </summary>
        /// <param name="content">The json content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="blobName">An optional blob name.</param>
        /// <returns></returns>
        public async Task<Uri> UploadJsonAsync(string content, CancellationToken cancellationToken, string? blobName = null)
        {
            var newBlobName = blobName ?? $"{Guid.NewGuid()}.json";
            var blob = GetBlobClient(newBlobName);

            await using var stream = content.ToStream();
            await UploadWithRetryAsync(() => blob.UploadAsync(stream, true, cancellationToken), cancellationToken);

            return blob.Uri;
        }

        /// <summary>
        /// Deletes the Cloud Storage Blob.
        /// </summary>
        /// <param name="blobName">Name of the BLOB.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task DeleteBlobAsync(string blobName, CancellationToken cancellationToken)
        {
            try
            {
                // Call the listing operation and return pages of the specified size.
                var resultSegment = _containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, blobName, cancellationToken).AsPages(default, 5000);

                // Enumerate the blobs returned for each page.
                await foreach (var blobPage in resultSegment)
                    foreach (var blobItem in blobPage.Values)
                        try
                        {
                            var blob = GetBlobClient(blobItem.Name);
                            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                        }
                        catch
                        {
                            // ignore.
                        }
            }
            catch
            {
                // ignore.
            }
        }

        /// <summary>
        /// Copies the folder.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task CopyFolderAsync(string from, string to, CancellationToken cancellationToken)
        {
            var blobClients = await GetBlobClientsAsync(from, cancellationToken);

            foreach (var blobClient in blobClients)
            {
                var newName = HttpUtility.UrlDecode(blobClient.Uri.AbsoluteUri.Replace(from, to).Replace(_containerClient.Uri.AbsoluteUri, ""));

                if (newName.StartsWith("/") && newName.Length > 1)
                    newName = newName[1..];

                var newBlob = GetBlobClient(newName);

                var lease = blobClient.GetBlobLeaseClient();
                await lease.AcquireAsync(TimeSpan.FromSeconds(-1), cancellationToken: cancellationToken);

                await newBlob.StartCopyFromUriAsync(blobClient.Uri, cancellationToken: cancellationToken);
                await lease.BreakAsync(cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Checks if the BLOB exists.
        /// </summary>
        /// <param name="blobName">Name of the file.</param>
        /// <returns></returns>
        public async Task<bool> BlobExistsAsync(string blobName) => await GetBlobClient(blobName).ExistsAsync();

        /// <summary>
        /// Checks if the BLOB exists.
        /// </summary>
        /// <param name="blobUri">The BLOB URI.</param>
        /// <returns></returns>
        public async Task<bool> BlobExistsAsync(Uri blobUri) => await GetBlobClient(blobUri).ExistsAsync();

        /// <summary>
        /// Downloads the blob as json content.
        /// </summary>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken)
        {
            var blob = GetBlobClient(blobName);
            return await DownloadBlobAsync(blob, cancellationToken);
        }

        /// <summary>
        /// Downloads the blob as stream.
        /// </summary>
        /// <param name="blobUri">The BLOB URI.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<Stream> DownloadAsync(Uri blobUri, CancellationToken cancellationToken)
        {
            var blob = GetBlobClient(blobUri);
            return await DownloadBlobAsync(blob, cancellationToken);
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <param name="blobName">Name of the BLOB.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<Dictionary<string, object>> GetPropertiesAsync(string blobName, CancellationToken cancellationToken) => await GetPropertiesAsync(GetBlobClient(blobName), cancellationToken);

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <param name="blobUri">The BLOB URI.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<Dictionary<string, object>> GetPropertiesAsync(Uri blobUri, CancellationToken cancellationToken) => await GetPropertiesAsync(GetBlobClient(blobUri), cancellationToken);

        /// <summary>
        /// Checks if the container exists.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ExistsAsync() => await _containerClient.ExistsAsync();

        /// <summary>
        /// Deletes the container.
        /// </summary>
        /// <returns></returns>
        public async Task DeleteAsync() => await _containerClient.DeleteAsync();

        /// <summary>
        /// Copies the container.
        /// </summary>
        /// <param name="destinationContainer">The destination container.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="Exception">Wrong container type.</exception>
        public async Task CopyAsync(IAzureBlobStorageContainer destinationContainer, CancellationToken cancellationToken)
        {
            if (destinationContainer is not AzureBlobStorageContainer container)
                throw new Exception("Wrong container type.");

            var blobClients = await GetBlobClientsAsync(string.Empty, cancellationToken);

            foreach (var blobClient in blobClients)
            {
                var newBlob = container._containerClient.GetBlobClient(blobClient.Name);

                var lease = blobClient.GetBlobLeaseClient();
                await lease.AcquireAsync(TimeSpan.FromSeconds(-1), cancellationToken: cancellationToken);

                await newBlob.StartCopyFromUriAsync(blobClient.Uri, cancellationToken: cancellationToken);
                await lease.BreakAsync(cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Updates the metadata.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task UpdateMetadataAsync(Dictionary<string, string> metadata, CancellationToken cancellationToken)
        {
            var properties = (await _containerClient.GetPropertiesAsync()).Value;

            foreach (var key in properties.Metadata.Keys)
            {
                if (metadata.ContainsKey(key))
                    continue;

                metadata[key] = properties.Metadata[key];
            }

            await _containerClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Copies the folder.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="destinationContainer">>The destination container</param>
        public async Task CopyFolderBetweenContainersAsync(string from, string to, IAzureBlobStorageContainer destinationContainer, CancellationToken cancellationToken)
        {
            if (destinationContainer is not AzureBlobStorageContainer container)
                throw new Exception("Wrong container type.");

            var blobClients = await GetBlobClientsAsync(from, cancellationToken);

            foreach (var blobClient in blobClients)
            {
                var newName = HttpUtility.UrlDecode(blobClient.Uri.AbsoluteUri.Replace(from, to).Replace(_containerClient.Uri.AbsoluteUri, string.Empty));

                if (newName.StartsWith("/") && newName.Length > 1)
                    newName = newName[1..];

                var newBlob = container.GetBlobClient(newName);

                var lease = blobClient.GetBlobLeaseClient();
                await lease.AcquireAsync(TimeSpan.FromSeconds(-1), cancellationToken: cancellationToken);

                await newBlob.StartCopyFromUriAsync(blobClient.Uri, cancellationToken: cancellationToken);
                await lease.BreakAsync(cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Return a blob client from the container
        /// </summary>
        /// <param name="blobName"></param>
        /// <returns></returns>
        public BlobClient GetBlobClient(string blobName) => _containerClient.GetBlobClient(blobName);

        /// <summary>
        /// Return a blob client from the provided URI
        /// </summary>
        /// <param name="blobUri">The BLOB URI.</param>
        /// <returns></returns>
        public BlobClient GetBlobClient(Uri blobUri)
        {
            var segments = blobUri.AbsolutePath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
            var containerIndex = Array.IndexOf(segments, _containerClient.Name);

            if (containerIndex == -1 || containerIndex == segments.Length - 1)
                throw new InvalidOperationException("Couldn't get the blob name from the provided Uri");

            var blobName = string.Join("/", segments, containerIndex + 1, segments.Length - containerIndex - 1);
            return GetBlobClient(blobName);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the blob clients.
        /// </summary>
        /// <param name="from">From.</param>
        /// <returns></returns>
        private async Task<List<BlobClient>> GetBlobClientsAsync(string from, CancellationToken cancellationToken)
        {
            var blobPages = _containerClient.GetBlobsByHierarchyAsync(
                traits: BlobTraits.None,
                states: BlobStates.None,
                delimiter: "/",
                prefix: from,
                cancellationToken: cancellationToken).AsPages(pageSizeHint: 5000);
            var blobs = new List<BlobClient>();

            await foreach (var blobPage in blobPages)
                foreach (var blobItem in blobPage.Values)
                    if (blobItem.IsBlob)
                        blobs.Add(GetBlobClient(blobItem.Blob.Name));
                    else
                        blobs.AddRange(await GetBlobClientsAsync(blobItem.Prefix, cancellationToken));

            return blobs;
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private async Task<Dictionary<string, object>> GetPropertiesAsync(BlobClient client, CancellationToken cancellationToken)
        {
            BlobProperties properties = await client.GetPropertiesAsync(cancellationToken: cancellationToken);

            return new Dictionary<string, object>
            {
                { nameof(properties.ContentType), properties.ContentType },
                { nameof(properties.ContentHash), properties.ContentHash },
                { nameof(properties.ContentLength), properties.ContentLength },
                { nameof(properties.ETag), properties.ETag },
                { nameof(properties.LastModified), properties.LastModified }
            };
        }

        /// <summary>
        /// Downloads the BLOB with retry logic and exponential backoff.
        /// </summary>
        /// <param name="blob">The BLOB.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private async Task<Stream> DownloadBlobAsync(BlobClient blob, CancellationToken cancellationToken)
        {
            int delayMs = _initialDelayMs;

            for (int attempt = 0; attempt <= _maxRetryAttempts; attempt++)
            {
                try
                {
                    return (await blob.DownloadAsync(cancellationToken)).Value.Content;
                }
                catch (Exception ex) when (IsTransientError(ex) && attempt < _maxRetryAttempts)
                {
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs *= 2; // Exponential backoff
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw; // Don't retry on cancellation
                }
            }

            // This line is unreachable but required for compilation
            throw new InvalidOperationException("Download retry logic failed unexpectedly.");
        }

        /// <summary>
        /// Executes an upload operation with exponential backoff retry logic.
        /// </summary>
        /// <param name="uploadOperation">The upload operation to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private async Task UploadWithRetryAsync(Func<Task> uploadOperation, CancellationToken cancellationToken)
        {
            int delayMs = _initialDelayMs;

            for (int attempt = 0; attempt <= _maxRetryAttempts; attempt++)
            {
                try
                {
                    await uploadOperation();
                    return;
                }
                catch (Exception ex) when (IsTransientError(ex) && attempt < _maxRetryAttempts)
                {
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs *= 2; // Exponential backoff
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw; // Don't retry on cancellation
                }
            }
        }

        /// <summary>
        /// Determines if an exception is transient and should be retried.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns>True if the exception is transient; otherwise, false.</returns>
        private static bool IsTransientError(Exception exception)
        {
            return exception switch
            {
                // Azure SDK transient errors
                Azure.RequestFailedException rfe when rfe.Status >= 500 => true,  // Server errors
                Azure.RequestFailedException rfe when rfe.Status == 408 => true, // Request timeout
                Azure.RequestFailedException rfe when rfe.Status == 429 => true, // Throttling

                // Network-level transient errors
                TimeoutException => true,
                HttpRequestException hre when hre.InnerException is TimeoutException => true,

                // Task/IO transient errors
                IOException ioe when IsNetworkError(ioe) => true,

                _ => false
            };
        }

        /// <summary>
        /// Determines if an IOException is network-related.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if the IOException is network-related; otherwise, false.</returns>
        private static bool IsNetworkError(IOException exception)
        {
            const string networkErrorMessages = "Network|Connection|Socket|Timeout";
            return System.Text.RegularExpressions.Regex.IsMatch(
                exception.Message,
                networkErrorMessages,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        #endregion
    }
}