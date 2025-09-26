﻿using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Paradigm.Enterprise.Services.BlobStorage.HealthCheck;
internal class AzureBlobStorageHealthCheck : IHealthCheck
{
    /// <summary>
    /// Gets the health check options.
    /// </summary>
    /// <value>
    /// The health check options.
    /// </value>
    private readonly AzureBlobStorageHealthCheckOptions _healthCheckOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobStorageHealthCheck"/> class.
    /// </summary>
    /// <param name="blobClient">The BLOB client.</param>
    /// <param name="healthCheckOptions">The health check options.</param>
    public AzureBlobStorageHealthCheck(AzureBlobStorageHealthCheckOptions healthCheckOptions)
    {
        this._healthCheckOptions = healthCheckOptions
            ?? throw new ArgumentNullException(nameof(healthCheckOptions), "The Azure Blob Storage Health Check Options argument is mandatory.");
    }

    /// <summary>
    /// Runs the health check, returning the status of the component being checked.
    /// </summary>
    /// <param name="context">A context object associated with the current execution.</param>
    /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that can be used to cancel the health check.</param>
    /// <returns>
    /// A <see cref="T:System.Threading.Tasks.Task`1" /> that completes when the health check has finished, yielding the status of the component being checked.
    /// </returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = new BlobServiceClient(_healthCheckOptions.ConnectionString ?? throw new NullReferenceException("The Azure Blob Storage Health Check needs at least a connection string."));

            if (this._healthCheckOptions.ContainerName != null)
            {
                var containerClient = blobClient.GetBlobContainerClient(this._healthCheckOptions.ContainerName);
                await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await blobClient
                    .GetBlobContainersAsync(cancellationToken: cancellationToken)
                    .AsPages(pageSizeHint: 1)
                    .GetAsyncEnumerator(cancellationToken: cancellationToken)
                    .MoveNextAsync()
                    .ConfigureAwait(false);
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }

    }
}