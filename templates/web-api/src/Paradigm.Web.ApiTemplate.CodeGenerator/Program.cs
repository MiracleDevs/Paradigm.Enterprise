using Paradigm.Web.ApiTemplate.CodeGenerator;
using Paradigm.Web.ApiTemplate.CodeGenerator.Generators;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) => { config.AddJsonFile("appsettings.json"); })
    .ConfigureServices((hostingContext, services) =>
    {
        services.AddTransient<JsonContextGenerator>();
        services.AddTransient<StoredProcedureMapperGenerator>();
        services.AddTransient<ProxiesGenerator>();
        services.AddSingleton<Application>();
    })
    .Build();

await host.Services.GetRequiredService<Application>().ExecuteAsync();