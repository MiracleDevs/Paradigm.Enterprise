using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace BeaconAr.DatabaseBootstrap;

internal static partial class SqlCmdRunner
{
    #region Public Methods

    public static async Task ExecuteAsync(
        string workingDirectory,
        string executable,
        int requiredMajorVersion,
        string connectionString,
        string scriptPath,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await VerifyVersionAsync(workingDirectory, executable, requiredMajorVersion, cancellationToken);
        var invocation = CreateInvocation(connectionString, scriptPath, timeout);
        await ProcessRunner.RunAsync(workingDirectory, timeout, executable, invocation.Arguments, cancellationToken, invocation.Environment);
    }

    internal static SqlCmdInvocation CreateInvocation(string connectionString, string scriptPath, TimeSpan timeout)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource) || string.IsNullOrWhiteSpace(builder.InitialCatalog))
            throw new InvalidOperationException("DatabaseConnection must specify Data Source and Initial Catalog for SQLCMD execution.");
        if (builder.Encrypt == SqlConnectionEncryptOption.Strict)
            throw new InvalidOperationException("SQLCMD 18 cannot preserve Encrypt=Strict; use Encrypt=True or execute pre-pre-deployment through a reviewed strict-capable client.");
        if (builder.Encrypt == SqlConnectionEncryptOption.Optional)
            throw new InvalidOperationException("SQLCMD 18 does not provide a portable way to preserve Encrypt=False; use Encrypt=True for bootstrap publication.");
        if (!string.IsNullOrWhiteSpace(builder.HostNameInCertificate))
            throw new InvalidOperationException("SQLCMD 18 cannot preserve HostNameInCertificate; remove it or execute pre-pre-deployment through a reviewed compatible client.");

        var timeoutSeconds = checked((int)Math.Ceiling(timeout.TotalSeconds));
        var arguments = new List<string>
        {
            "-b",
            "-V", "16",
            "-S", builder.DataSource,
            "-d", builder.InitialCatalog,
            "-i", scriptPath,
            "-l", timeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "-t", timeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        var environment = new Dictionary<string, string?>();

        if (builder.IntegratedSecurity)
        {
            arguments.Add("-E");
        }
        else
        {
            if (builder.Authentication != SqlAuthenticationMethod.NotSpecified)
                throw new InvalidOperationException("The configured SQL authentication method is not supported by the pinned SQLCMD contract.");
            if (string.IsNullOrWhiteSpace(builder.UserID) || string.IsNullOrEmpty(builder.Password))
                throw new InvalidOperationException("DatabaseConnection must specify either Integrated Security or SQL user credentials for SQLCMD execution.");

            arguments.Add("-U");
            arguments.Add(builder.UserID);
            environment["SQLCMDPASSWORD"] = builder.Password;
        }

        arguments.Add("-N");
        if (builder.TrustServerCertificate)
            arguments.Add("-C");

        return new SqlCmdInvocation(arguments, environment);
    }

    #endregion

    #region Private Methods

    private static async Task VerifyVersionAsync(
        string workingDirectory,
        string executable,
        int requiredMajorVersion,
        CancellationToken cancellationToken)
    {
        var help = await ProcessRunner.RunAndCaptureAsync(
            workingDirectory,
            TimeSpan.FromSeconds(15),
            executable,
            ["-?"],
            new HashSet<int> { 0, 1 },
            cancellationToken);
        var match = VersionLine().Match(help);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var actualMajorVersion))
            throw new InvalidOperationException("The SQLCMD version could not be determined.");
        if (actualMajorVersion != requiredMajorVersion)
            throw new InvalidOperationException($"SQLCMD major version {requiredMajorVersion} is required; found version {actualMajorVersion}.");
    }

    [GeneratedRegex(@"(?im)^Version\s+(\d+)\.", RegexOptions.CultureInvariant)]
    private static partial Regex VersionLine();

    #endregion

    internal sealed record SqlCmdInvocation(
        IReadOnlyCollection<string> Arguments,
        IReadOnlyDictionary<string, string?> Environment);
}
