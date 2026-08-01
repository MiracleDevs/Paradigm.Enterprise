using Aspire.Hosting;
using BeaconAr.AppHost;

var root = FindExampleRoot();
var marker = Path.Combine(root, "aspire.config.json");
if (!File.Exists(marker))
{
    throw new InvalidOperationException($"Repository marker not found at '{marker}'.");
}

DotEnvLoader.Load(Path.Combine(root, ".env"));
if (Environment.GetEnvironmentVariable("Parameters__database-password") is null &&
    Environment.GetEnvironmentVariable("Database__Password") is { } databasePassword)
{
    Environment.SetEnvironmentVariable("Parameters__database-password", databasePassword);
}

var builder = DistributedApplication.CreateBuilder(args);
var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var mode = builder.Configuration["Database:Mode"] ?? "Managed";
var databaseName = builder.Configuration["Database:Name"] ?? "BeaconAr";
var publishSetting = builder.Configuration["Database:PublishOnStart"];
var sqlCmdPath = builder.Configuration["Database:SqlCmdPath"] ?? "sqlcmd";
var sqlCmdMajorVersion = builder.Configuration["Database:SqlCmdMajorVersion"] ?? "18";
var publishOnStart = publishSetting is null
    ? mode.Equals("Managed", StringComparison.OrdinalIgnoreCase)
    : bool.TryParse(publishSetting, out var publish)
        ? publish
        : throw new InvalidOperationException("Database:PublishOnStart must be true or false.");

if (!provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Beacon AR foundation supports only Database:Provider=SqlServer.");
}

if (string.IsNullOrWhiteSpace(databaseName))
{
    throw new InvalidOperationException("Database:Name is required.");
}

IResourceBuilder<IResourceWithConnectionString> database;
if (mode.Equals("Managed", StringComparison.OrdinalIgnoreCase))
{
    var configuredPassword = builder.Configuration["Database:Password"];
    if (string.IsNullOrWhiteSpace(configuredPassword) || configuredPassword.StartsWith('<'))
    {
        throw new InvalidOperationException("Database:Password must be set locally for Managed mode.");
    }

    var password = builder.AddParameter("database-password", secret: true);
    database = builder.AddSqlServer("sqlserver", password)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .AddDatabase("databaseconnection", databaseName);
}
else if (mode.Equals("External", StringComparison.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(builder.Configuration["ConnectionStrings:DatabaseConnection"]))
    {
        throw new InvalidOperationException("ConnectionStrings:DatabaseConnection is required for External mode.");
    }

    database = builder.AddConnectionString("databaseconnection");
}
else
{
    throw new InvalidOperationException("Database:Mode must be Managed or External.");
}

var api = builder.AddProject<Projects.BeaconAr_WebApi>("webapi")
    .WithReference(database, "DatabaseConnection")
    .WaitFor(database);

if (publishOnStart)
{
    var bootstrap = builder.AddProject<Projects.BeaconAr_DatabaseBootstrap>("database-bootstrap")
        .WithReference(database, "DatabaseConnection")
        .WithEnvironment("Database__Mode", mode)
        .WithEnvironment("Database__Name", databaseName)
        .WithEnvironment("Database__PublishOnStart", "true")
        .WithEnvironment("Database__SqlCmdPath", sqlCmdPath)
        .WithEnvironment("Database__SqlCmdMajorVersion", sqlCmdMajorVersion)
        .WaitFor(database);
    api.WaitForCompletion(bootstrap);
}

builder.Build().Run();

static string FindExampleRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "aspire.config.json")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new DirectoryNotFoundException("Beacon AR example root was not found.");
}
