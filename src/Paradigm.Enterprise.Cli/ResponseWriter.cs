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

        if (response.Command is "help" or "version")
        {
            foreach (var result in response.Results)
                await output.WriteLineAsync(result.Detail);
            return;
        }

        await output.WriteLineAsync($"{response.Command}: {response.Status}");
        if (!response.Command.StartsWith("api ", StringComparison.Ordinal))
            foreach (var package in response.Packages)
                await output.WriteLineAsync($"package {package.Name} {package.Version} [{Path.GetFileName(package.Project)}]");
        IReadOnlyList<ResultItem> textResults = response.Command == "packages check"
            ? response.Results
                .DistinctBy(result => (result.Kind, result.Name, result.Detail, result.Package, result.Version))
                .Take(50)
                .ToArray()
            : response.Results;
        foreach (var result in textResults)
        {
            var owner = result.Package is null ? "" : $" [{result.Package} {result.Version}]";
            await output.WriteLineAsync($"{result.Kind} {result.Name}{owner}");
            foreach (var line in result.Detail.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                await output.WriteLineAsync($"  {line}");
        }
        if (response.Command == "packages check" && textResults.Count < response.Results.Count)
            await output.WriteLineAsync(
                $"... {response.Results.Count - textResults.Count} duplicate or additional package entries omitted; " +
                "use --format json for the complete result.");
        foreach (var diagnostic in response.Diagnostics)
            await output.WriteLineAsync($"{diagnostic.Code} {diagnostic.Severity}: {diagnostic.Message}{(diagnostic.Location is null ? "" : $" [{diagnostic.Location}]")}");
        if (response.Guide is not null)
        {
            await output.WriteLineAsync($"guide {response.Guide.Symbol}");
            await output.WriteLineAsync($"  pattern: {response.Guide.RecommendedPattern}");
            await WriteLines("generic", response.Guide.GenericParameters, output);
            await WriteLines("required", response.Guide.RequiredMembers, output);
            await WriteLines("optional", response.Guide.OptionalHooks, output);
            await WriteLines("discovery", response.Guide.DiscoveryAndRegistration, output);
            await WriteLines("caution", response.Guide.Cautions, output);
            await WriteLines("verify", response.Guide.Verification, output);
        }
    }

    private static async Task WriteLines(string label, IEnumerable<string> values, TextWriter output)
    {
        foreach (var value in values)
            await output.WriteLineAsync($"  {label}: {value}");
    }
}
