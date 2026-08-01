using Microsoft.Data.SqlClient;
using BeaconAr.DatabaseBootstrap;
using Microsoft.Extensions.Logging;

using var shutdown = new CancellationTokenSource();
using var loggerFactory = LoggerFactory.Create(builder => builder.AddJsonConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffK";
    options.UseUtcTimestamp = true;
}));
var logger = loggerFactory.CreateLogger("BeaconAr.DatabaseBootstrap");
ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};
EventHandler processExitHandler = (_, _) => shutdown.Cancel();
Console.CancelKeyPress += cancelHandler;
AppDomain.CurrentDomain.ProcessExit += processExitHandler;

try
{
    await RunBootstrapAsync(logger, shutdown.Token);
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
    AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
}

static async Task RunBootstrapAsync(ILogger logger, CancellationToken cancellationToken)
{
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DatabaseConnection")
        ?? throw new InvalidOperationException("ConnectionStrings__DatabaseConnection is required.");
    var mode = Environment.GetEnvironmentVariable("Database__Mode") ?? "Managed";
    var publish = bool.TryParse(Environment.GetEnvironmentVariable("Database__PublishOnStart"), out var enabled) && enabled;
    if (!publish)
    {
        BootstrapPhase.LogSkipped(logger, "bootstrap", "publication-disabled");
        return;
    }

    var sqlCmdPath = Environment.GetEnvironmentVariable("Database__SqlCmdPath") ?? "sqlcmd";
    var sqlCmdMajorVersionValue = Environment.GetEnvironmentVariable("Database__SqlCmdMajorVersion") ?? "18";
    if (!int.TryParse(sqlCmdMajorVersionValue, out var sqlCmdMajorVersion) || sqlCmdMajorVersion < 1)
        throw new InvalidOperationException("Database__SqlCmdMajorVersion must be a positive integer.");

    var root = FindExampleRoot();
    var sqlProject = Path.Combine(root, "src", "database", "BeaconAr.Database.sqlproj");
    var artifactDirectory = Path.Combine(root, "artifacts", "database");
    Directory.CreateDirectory(artifactDirectory);

    await BootstrapPhase.RunAsync(logger, "database-wait", TimeSpan.FromMinutes(2),
        token => WaitForDatabaseAsync(connectionString, TimeSpan.FromMinutes(2), token), cancellationToken);

    var baseline = Path.Combine(root, "src", "database", "bootstrap", "BeaconAr.bacpac");
    if (File.Exists(baseline))
    {
        await BootstrapPhase.RunAsync(logger, "baseline-decision", TimeSpan.FromMinutes(15), async token =>
        {
            if (!mode.Equals("Managed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A BACPAC baseline can never be imported in External mode.");

            if (!await IsDatabaseEmptyAsync(connectionString, TimeSpan.FromMinutes(2), token))
                throw new InvalidOperationException("The managed database is not provably empty; baseline import was refused.");

            await ProcessRunner.RunAsync(root, TimeSpan.FromMinutes(15), "dotnet", ["tool", "run", "sqlpackage", "/Action:Import", $"/SourceFile:{baseline}", $"/TargetConnectionString:{connectionString}"], token);
        }, cancellationToken);
    }
    else
        BootstrapPhase.LogSkipped(logger, "baseline-decision", "baseline-not-present");

    var expectedDacpac = Path.Combine(artifactDirectory, "BeaconAr.Database.dacpac");
    var report = Path.Combine(artifactDirectory, "BeaconAr.Database.deployreport.xml");
    await BootstrapPhase.RunAsync(logger, "dacpac-build", TimeSpan.FromMinutes(15), async token =>
    {
        File.Delete(expectedDacpac);
        File.Delete(report);
        await ProcessRunner.RunAsync(root, TimeSpan.FromMinutes(15), "dotnet", ["build", sqlProject, "--configuration", "Release", $"/p:OutputPath={artifactDirectory}{Path.DirectorySeparatorChar}"], token);
        var dacpacs = Directory.GetFiles(artifactDirectory, "*.dacpac", SearchOption.TopDirectoryOnly);
        if (dacpacs.Length != 1 || !StringComparer.OrdinalIgnoreCase.Equals(dacpacs[0], expectedDacpac))
            throw new InvalidOperationException($"Expected exactly one BeaconAr.Database.dacpac, found {dacpacs.Length} DACPAC artifact(s).");
    }, cancellationToken);

    var prePre = Path.Combine(root, "src", "database", "scripts", "prepredeployment", "PrePreDeployment.sql");
    await BootstrapPhase.RunAsync(logger, "pre-pre", TimeSpan.FromMinutes(15),
        token => SqlCmdRunner.ExecuteAsync(root, sqlCmdPath, sqlCmdMajorVersion, connectionString, prePre, TimeSpan.FromMinutes(15), token), cancellationToken);

    await BootstrapPhase.RunAsync(logger, "deploy-report", TimeSpan.FromMinutes(15),
        token => ProcessRunner.RunAsync(root, TimeSpan.FromMinutes(15), "dotnet", ["tool", "run", "sqlpackage", "/Action:DeployReport", $"/SourceFile:{expectedDacpac}", $"/TargetConnectionString:{connectionString}", $"/OutputPath:{report}"], token), cancellationToken);
    await BootstrapPhase.RunAsync(logger, "publish", TimeSpan.FromMinutes(15),
        token => ProcessRunner.RunAsync(root, TimeSpan.FromMinutes(15), "dotnet", ["tool", "run", "sqlpackage", "/Action:Publish", $"/SourceFile:{expectedDacpac}", $"/TargetConnectionString:{connectionString}"], token), cancellationToken);
    await BootstrapPhase.RunAsync(logger, "probe", TimeSpan.FromMinutes(2),
        token => ProbeSchemaAsync(connectionString, TimeSpan.FromMinutes(2), token), cancellationToken);
}

static async Task WaitForDatabaseAsync(string connectionString, TimeSpan timeout, CancellationToken cancellationToken)
{
    using var deadline = new CancellationTokenSource(timeout);
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
    Exception? lastError = null;
    try
    {
        while (true)
        {
            linked.Token.ThrowIfCancellationRequested();
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(linked.Token);
                return;
            }
            catch (Exception exception) when (exception is SqlException or InvalidOperationException)
            {
                lastError = exception;
                await Task.Delay(TimeSpan.FromSeconds(2), linked.Token);
            }
        }
    }
    catch (OperationCanceledException exception) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
    {
        throw new TimeoutException("The database did not become ready within the configured deadline.", lastError ?? exception);
    }
}

static async Task<bool> IsDatabaseEmptyAsync(string connectionString, TimeSpan timeout, CancellationToken cancellationToken)
{
    const string sql = "SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM sys.objects WHERE is_ms_shipped = 0) AND NOT EXISTS (SELECT 1 FROM sys.types WHERE is_user_defined = 1) AND NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name NOT IN (N'dbo', N'guest', N'sys', N'INFORMATION_SCHEMA')) THEN 1 ELSE 0 END;";
    using var deadline = new CancellationTokenSource(timeout);
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync(linked.Token);
    await using var command = new SqlCommand(sql, connection) { CommandTimeout = checked((int)Math.Ceiling(timeout.TotalSeconds)) };
    return Convert.ToInt32(await command.ExecuteScalarAsync(linked.Token), System.Globalization.CultureInfo.InvariantCulture) == 1;
}

static async Task ProbeSchemaAsync(string connectionString, TimeSpan timeout, CancellationToken cancellationToken)
{
    const string sql = "SELECT CASE WHEN OBJECT_ID(N'[dbo].[Product]', N'U') IS NOT NULL AND (SELECT COUNT(*) FROM [dbo].[QuoteStatus]) = 5 AND (SELECT COUNT(*) FROM [dbo].[SalesOrderStatus]) = 6 THEN 1 ELSE 0 END;";
    using var deadline = new CancellationTokenSource(timeout);
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync(linked.Token);
    await using var command = new SqlCommand(sql, connection) { CommandTimeout = checked((int)Math.Ceiling(timeout.TotalSeconds)) };
    if (Convert.ToInt32(await command.ExecuteScalarAsync(linked.Token), System.Globalization.CultureInfo.InvariantCulture) != 1)
        throw new InvalidOperationException("Schema publication probe failed.");
}

static string FindExampleRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "aspire.config.json")))
        directory = directory.Parent;

    return directory?.FullName ?? throw new DirectoryNotFoundException("Beacon AR example root was not found.");
}
