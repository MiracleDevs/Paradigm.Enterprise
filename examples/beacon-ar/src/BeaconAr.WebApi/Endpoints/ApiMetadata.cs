using System.Reflection;

namespace BeaconAr.WebApi.Endpoints;

internal static class ApiMetadata
{
    #region Constants

    public const string Name = "Beacon AR API";
    public const string OpenApiVersion = "v1";

    #endregion

    #region Properties

    public static string ProductVersion { get; } = ResolveProductVersion();

    #endregion

    #region Private Methods

    private static string ResolveProductVersion()
    {
        Assembly assembly = typeof(ApiMetadata).Assembly;
        string? informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
            return informationalVersion.Split('+', 2)[0];

        return assembly.GetName().Version?.ToString(3) ?? "unknown";
    }

    #endregion
}
