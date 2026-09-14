using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.JavaScript;

namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Provides AppHost composition helpers for existing Angular clients.
/// </summary>
public static class AngularClientHostingExtensions
{
    #region Public Methods

    /// <summary>
    /// Adds an Angular JavaScript application and references its required APIs.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="options">The Angular client options.</param>
    /// <param name="apis">The APIs that the client consumes.</param>
    /// <returns>The configured Angular client resource.</returns>
    public static IResourceBuilder<JavaScriptAppResource> AddParadigmAngularClient(
        this IDistributedApplicationBuilder builder,
        ParadigmAngularClientOptions options,
        params IResourceBuilder<ProjectResource>[] apis)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(apis);
        Validate(options);

        var client = builder.AddJavaScriptApp(options.Name, options.WorkingDirectory)
            .WithRunScript(options.RunScript)
            .WithHttpEndpoint(
                port: options.HostPort,
                targetPort: options.TargetPort,
                name: options.EndpointName,
                env: options.PortEnvironmentVariable,
                isProxied: false);

        foreach (var api in apis)
        {
            ArgumentNullException.ThrowIfNull(api);
            client.WithReference(api);
            if (options.WaitForApis)
                client.WaitFor(api);
        }

        if (options.ExposeExternalEndpoints)
            client.WithExternalHttpEndpoints();

        return client;
    }

    #endregion

    #region Private Methods

    private static void Validate(ParadigmAngularClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Name))
            throw new InvalidOperationException("An Angular client resource name is required.");
        if (string.IsNullOrWhiteSpace(options.WorkingDirectory))
            throw new InvalidOperationException("An Angular client working directory is required.");
        if (string.IsNullOrWhiteSpace(options.RunScript))
            throw new InvalidOperationException("An Angular client run script is required.");
        if (string.IsNullOrWhiteSpace(options.EndpointName))
            throw new InvalidOperationException("An Angular client endpoint name is required.");
        if (string.IsNullOrWhiteSpace(options.PortEnvironmentVariable))
            throw new InvalidOperationException("An Angular client port environment variable is required.");
        if (options.HostPort is <= 0 or > 65535)
            throw new InvalidOperationException("An Angular client host port must be between 1 and 65535.");
        if (options.TargetPort is <= 0 or > 65535)
            throw new InvalidOperationException("An Angular client target port must be between 1 and 65535.");
    }

    #endregion
}
