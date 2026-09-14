using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Provides reusable Azure Storage emulator hosting recipes.
/// </summary>
public static class BlobStorageHostingExtensions
{
    #region Public Methods

    /// <summary>
    /// Adds an Azurite-backed blob resource or an external connection-string resource.
    /// </summary>
    /// <param name="builder">The Aspire application builder.</param>
    /// <param name="options">Blob storage configuration.</param>
    /// <returns>A consumer reference for the blob connection.</returns>
    public static ParadigmResourceReference AddParadigmBlobStorage(
        this IDistributedApplicationBuilder builder,
        ParadigmBlobStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options.ConnectionName, nameof(options.ConnectionName));

        if (options.Mode == ParadigmResourceMode.External)
        {
            var external = builder.AddConnectionString(options.ConnectionName);
            return new ParadigmResourceReference(project => project.WithReference(external));
        }

        Validate(options.StorageName, nameof(options.StorageName));
        Validate(options.BlobResourceName, nameof(options.BlobResourceName));

        var storage = builder.AddAzureStorage(options.StorageName)
            .RunAsEmulator(emulator =>
            {
                emulator.WithLifetime(ContainerLifetime.Persistent)
                    .WithDataVolume(options.DataVolumeName);
                if (options.BlobPort is not null)
                    emulator.WithBlobPort(options.BlobPort.Value);
            });
        var blobs = storage.AddBlobs(options.BlobResourceName);
        var connection = builder.AddConnectionString(options.ConnectionName, blobs.Resource.ConnectionStringExpression);
        return new ParadigmResourceReference(project => project.WithReference(connection).WaitFor(storage));
    }

    #endregion

    #region Private Methods

    private static void Validate(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{name} is required.");
    }

    #endregion
}
