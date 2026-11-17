using System.Reflection;

namespace Paradigm.Enterprise.Cli.Tests;

internal static class TestReleaseVersion
{
    #region Properties

    // MSBuild supplies the expected release independently of the CLI assembly under test.
    internal static string PackageVersion { get; } = ReadPackageVersion();

    // The existing --version contract displays three assembly-version components.
    internal static string CliVersion
    {
        get
        {
            var version = Version.Parse(PackageVersion.Split(['-', '+'], 2)[0]);
            return new Version(version.Major, version.Minor, Math.Max(version.Build, 0)).ToString(3);
        }
    }

    #endregion

    #region Private Methods

    private static string ReadPackageVersion()
    {
        var version = typeof(TestReleaseVersion).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(attribute => attribute.Key == "ParadigmEnterpriseVersion")?.Value;
        return !string.IsNullOrWhiteSpace(version)
            ? version
            : throw new InvalidOperationException("The test build must supply ParadigmEnterpriseVersion from build/Paradigm.Version.props.");
    }

    #endregion
}