using Aspire.Hosting;

namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Creates an Aspire application builder after loading local repository configuration.
/// </summary>
public static class ParadigmDistributedApplication
{
    /// <summary>
    /// Loads the repository-root <c>.env</c> file without replacing process values and creates an Aspire builder.
    /// </summary>
    /// <param name="args">Application command-line arguments.</param>
    /// <returns>A configured distributed application builder.</returns>
    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        RepositoryEnvironmentLoader.Load();
        return DistributedApplication.CreateBuilder(args);
    }
}
