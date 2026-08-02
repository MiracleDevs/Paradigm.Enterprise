using BeaconAr.CodeGenerator;
using BeaconAr.CodeGenerator.Generators;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Directory.SetCurrentDirectory(FindGeneratorProjectDirectory());
if (args.Length > 0 && string.Equals(args[0], "openapi-typescript", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length != 3)
        throw new ArgumentException("Usage: openapi-typescript <openapi-document> <output-file>", nameof(args));
    await OpenApiTypeScriptGenerator.GenerateAsync(args[1], args[2]);
    return;
}
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json");
    })
    .ConfigureServices((hostingContext, services) =>
    {
        services.AddTransient<JsonContextGenerator>();
        services.AddTransient<StoredProcedureMapperGenerator>();
        services.AddSingleton<Application>();
    })
    .Build();

await host.Services.GetRequiredService<Application>().ExecuteAsync(args);

static string FindGeneratorProjectDirectory()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BeaconAr.CodeGenerator.csproj")))
        directory = directory.Parent;

    return directory?.FullName ?? throw new DirectoryNotFoundException("Beacon AR code-generator project directory was not found.");
}
