using Microsoft.DemoManagementSystem.WebApi.CodeGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Paradigm.Enterprise.CodeGenerator.Generators;

if (args.Contains("--help"))
{
    ShowHelp();
    return;
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("appsettings.json");
        config.AddCommandLine(args);
    })
    .ConfigureServices((hostingContext, services) =>
    {
        services.AddTransient<JsonContextGenerator>();
        services.AddTransient<StoredProcedureMapperGenerator>();
        services.AddTransient<ProxiesGenerator>();
        services.AddSingleton<Application>();
    })
    .Build();

await host.Services.GetRequiredService<Application>().ExecuteAsync();

static void ShowHelp()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  Paradigm.Enterprise.CodeGenerator --ProjectName <projectNameValue> --ProvidersAssemblyPath <providersAssemblyPathValue> --DataAssemblyPath <dataAssemblyPathValue> --ProxiesOutput <proxiesOutputValue> --SwaggerUrl <swaggerUrlValue>");
    Console.WriteLine();
    Console.WriteLine("Parameters:");
    Console.WriteLine("  --ProjectName    The current project name (required). Example: 'MySampleProject'");
    Console.WriteLine("  --ProvidersAssemblyPath    The relative path to the Providers assembly (required). Example: '../MySampleProject.Providers'");
    Console.WriteLine("  --DataAssemblyPath    The relative path to the Data assembly (required). Example: '../MySampleProject.Data'");
    Console.WriteLine("  --ProxiesOutput    The path to the folder where the proxies file is stored (required). Example: '../../client/web/src/app/services/http/clients'");
    Console.WriteLine("  --SwaggerUrl    The URL to get the Open API documentation JSON (optional). Example: 'https://localhost:7020/swagger/v1/swagger.json'");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --help      Show command line help.");
}