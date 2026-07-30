using System.Text.Json;

namespace Paradigm.Enterprise.Cli;

internal static class ResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task WriteAsync(CommandResponse response, OutputFormat format, TextWriter output)
    {
        if (format == OutputFormat.Json)
        {
            await output.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions));
            return;
        }

        await output.WriteLineAsync($"{response.Command}: {response.Status}");
        if (!response.Command.StartsWith("api ", StringComparison.Ordinal))
            foreach (var package in response.Packages)
                await output.WriteLineAsync($"package {package.Name} {package.Version} [{Path.GetFileName(package.Project)}]");
        foreach (var result in response.Results)
        {
            var owner = result.Package is null ? "" : $" [{result.Package} {result.Version}]";
            await output.WriteLineAsync($"{result.Kind} {result.Name}{owner}");
            foreach (var line in result.Detail.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                await output.WriteLineAsync($"  {line}");
        }
        foreach (var diagnostic in response.Diagnostics)
            await output.WriteLineAsync($"{diagnostic.Code} {diagnostic.Severity}: {diagnostic.Message}{(diagnostic.Location is null ? "" : $" [{diagnostic.Location}]")}");
    }
}
