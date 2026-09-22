namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Defines the AppHost composition settings for an existing Angular client.
/// </summary>
public sealed class ParadigmAngularClientOptions
{
    #region Properties

    /// <summary>
    /// Gets the unique Aspire resource name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the client working directory relative to the AppHost project.
    /// </summary>
    public required string WorkingDirectory { get; init; }

    /// <summary>
    /// Gets the package script used to start the client.
    /// </summary>
    public string RunScript { get; init; } = "start";

    /// <summary>
    /// Gets the endpoint name exposed by the client.
    /// </summary>
    public string EndpointName { get; init; } = "http";

    /// <summary>
    /// Gets the optional fixed host port. A null value allows Aspire to assign one.
    /// </summary>
    public int? HostPort { get; init; }

    /// <summary>
    /// Gets the port used by the Angular development server inside the client process.
    /// </summary>
    public int TargetPort { get; init; } = 4200;

    /// <summary>
    /// Gets the environment variable used to pass the development-server port.
    /// </summary>
    public string PortEnvironmentVariable { get; init; } = "PORT";

    /// <summary>
    /// Gets a value indicating whether the client endpoint uses HTTPS instead of HTTP.
    /// </summary>
    public bool UseHttps { get; init; }

    /// <summary>
    /// Gets a value indicating whether the client endpoint is externally browsable.
    /// </summary>
    public bool ExposeExternalEndpoints { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the client waits for referenced APIs before starting.
    /// </summary>
    public bool WaitForApis { get; init; }

    #endregion
}
