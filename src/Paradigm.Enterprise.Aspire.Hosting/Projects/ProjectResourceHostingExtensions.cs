using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Provides infrastructure, setting, and secret forwarding helpers for project resources.
/// </summary>
public static class ProjectResourceHostingExtensions
{
    #region Public Methods

    /// <summary>
    /// Attaches infrastructure references and readiness dependencies to a project resource.
    /// </summary>
    /// <param name="project">The consuming project resource.</param>
    /// <param name="references">Infrastructure references to attach.</param>
    /// <returns>The configured project resource.</returns>
    public static IResourceBuilder<ProjectResource> WithParadigmInfrastructure(
        this IResourceBuilder<ProjectResource> project,
        params ParadigmResourceReference[] references)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(references);

        foreach (var reference in references)
        {
            ArgumentNullException.ThrowIfNull(reference);
            reference.ConfigureConsumer(project);
        }

        return project;
    }

    /// <summary>
    /// Forwards configured optional values to a project resource.
    /// </summary>
    /// <param name="project">The consuming project resource.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="settings">Settings to forward when present.</param>
    /// <returns>The configured project resource.</returns>
    public static IResourceBuilder<ProjectResource> WithOptionalSettings(
        this IResourceBuilder<ProjectResource> project,
        IConfiguration configuration,
        params string[] settings)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(settings);

        foreach (var setting in settings)
        {
            if (string.IsNullOrWhiteSpace(setting))
                throw new ArgumentException("Setting names cannot be empty.", nameof(settings));

            var value = configuration[setting];
            if (!string.IsNullOrWhiteSpace(value))
                project.WithEnvironment(ToEnvironmentVariableName(setting), value);
        }

        return project;
    }

    /// <summary>
    /// Forwards configured optional secrets through secret Aspire parameters.
    /// </summary>
    /// <param name="project">The consuming project resource.</param>
    /// <param name="builder">The Aspire application builder.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="secrets">Configuration keys mapped to secret parameter names.</param>
    /// <returns>The configured project resource.</returns>
    public static IResourceBuilder<ProjectResource> WithOptionalSecrets(
        this IResourceBuilder<ProjectResource> project,
        IDistributedApplicationBuilder builder,
        IConfiguration configuration,
        IReadOnlyDictionary<string, string> secrets)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(secrets);

        foreach (var secret in secrets)
        {
            if (string.IsNullOrWhiteSpace(secret.Key))
                throw new ArgumentException("Secret configuration keys cannot be empty.", nameof(secrets));
            if (string.IsNullOrWhiteSpace(secret.Value))
                throw new ArgumentException("Secret parameter names cannot be empty.", nameof(secrets));

            var value = configuration[secret.Key];
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var parameter = builder.AddParameter(secret.Value, value, secret: true);
            project.WithEnvironment(ToEnvironmentVariableName(secret.Key), parameter);
        }

        return project;
    }

    #endregion

    #region Private Methods

    private static string ToEnvironmentVariableName(string configurationKey) =>
        configurationKey.Replace(":", "__", StringComparison.Ordinal);

    #endregion
}
