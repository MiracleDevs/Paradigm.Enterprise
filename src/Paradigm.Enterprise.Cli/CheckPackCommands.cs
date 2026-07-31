using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record CheckPackRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("packId")] string PackId,
    [property: JsonPropertyName("packVersion")] string PackVersion,
    [property: JsonPropertyName("projects")] IReadOnlyList<CheckProject> Projects,
    [property: JsonPropertyName("packages")] IReadOnlyList<PackageInfo> Packages,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string> Metadata,
    [property: JsonPropertyName("suppressions")] IReadOnlyList<ConfiguredSuppression> Suppressions);

internal sealed record CheckProject(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("framework")] string Framework);

internal sealed record CheckPackResponse(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("packId")] string PackId,
    [property: JsonPropertyName("packVersion")] string PackVersion,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<Diagnostic> Diagnostics);

internal sealed record CheckPackRunResult(
    IReadOnlyList<Diagnostic> Diagnostics,
    bool Executed);

internal sealed class ChecksListCommandHandler(
    IProjectResolutionService projects,
    IConfigurationService configuration) : CliCommandHandler<ChecksListOptions>
{
    public override string Route => "checks list";

    public override Task<CommandResponse> ExecuteAsync(ChecksListOptions options, CancellationToken cancellationToken)
    {
        var selection = projects.Resolve(options.Project);
        var configured = configuration.Load(selection, options.Config);
        var packs = configured.Configuration.CheckPacks.Select(x =>
            new CheckPackInfo(x.Id, x.Version, x.Executable, x.DiagnosticPrefix)).ToArray();
        var results = packs.Select(x => new ResultItem("check-pack", x.Id,
            $"version={x.Version}; executable={x.Executable}; prefix={x.DiagnosticPrefix}"));
        return Task.FromResult(ResponseFactory.Create(Route, [], results, configured.Diagnostics,
            checks: new(packs, [])));
    }
}

internal sealed class ChecksRunCommandHandler(
    IProjectResolutionService projects,
    IAssetService assets,
    IConfigurationService configuration,
    CheckPackRunner runner) : CliCommandHandler<ChecksRunOptions>
{
    public override string Route => "checks run";

    public override async Task<CommandResponse> ExecuteAsync(
        ChecksRunOptions options,
        CancellationToken cancellationToken)
    {
        var selection = projects.Resolve(options.Project);
        var configured = configuration.Load(selection, options.Config);
        var diagnostics = configured.Diagnostics.ToList();
        diagnostics.AddRange(SuppressionPolicy.Expired(configured.Configuration.Suppressions,
            configured.Configuration.Path));
        var selected = configured.Configuration.CheckPacks
            .Where(x => options.Pack is null || x.Id.Equals(options.Pack, StringComparison.Ordinal))
            .ToArray();
        if (options.Pack is not null && selected.Length == 0)
            diagnostics.Add(new("PE5001", "error", $"Check pack '{options.Pack}' is not registered.",
                configured.Configuration.Path));

        var projectsAndFrameworks = new List<CheckProject>();
        var packages = new List<PackageInfo>();
        if (!diagnostics.Any(x => x.Severity == "error"))
            foreach (var project in selection.Projects)
            {
                try
                {
                    var asset = assets.Read(project, options.Framework, true);
                    projectsAndFrameworks.Add(new(project, asset.Framework));
                    packages.AddRange(asset.Packages);
                }
                catch (AssetsException exception)
                {
                    diagnostics.Add(new("PE1002", "error", exception.Message, project));
                }
            }

        var executed = new List<string>();
        foreach (var pack in selected)
        {
            if (diagnostics.Any(x => x.Severity == "error" && x.Code is "PE5001" or "PE1002"))
                break;
            var request = new CheckPackRequest("1.0", pack.Id, pack.Version, projectsAndFrameworks, packages,
                new Dictionary<string, string>
                {
                    ["workspaceRoot"] = configured.Configuration.WorkspaceRoot,
                    ["configurationPath"] = configured.Configuration.Path
                },
                configured.Configuration.Suppressions);
            var result = await runner.RunAsync(pack, configured.Configuration.WorkspaceRoot, request, cancellationToken);
            if (result.Executed)
                executed.Add(pack.Id);
            diagnostics.AddRange(result.Diagnostics.Where(x =>
                !SuppressionPolicy.IsSuppressed(x, configured.Configuration.Suppressions)));
        }

        var packInfos = selected.Select(x =>
            new CheckPackInfo(x.Id, x.Version, x.Executable, x.DiagnosticPrefix)).ToArray();
        return ResponseFactory.Create(Route, packages, [], diagnostics, checks: new(packInfos, executed));
    }
}

internal sealed class CheckPackRunner
{
    public async Task<CheckPackRunResult> RunAsync(
        ConfiguredCheckPack pack,
        string workspaceRoot,
        CheckPackRequest request,
        CancellationToken cancellationToken)
    {
        if (!PathContainment.TryResolveWithin(
                workspaceRoot, pack.Executable, out var executable, out var containmentError))
            return new([new("PE5001", "error",
                $"Check pack '{pack.Id}' executable is not contained by the canonical workspace: {containmentError}.",
                executable)], false);
        if (!File.Exists(executable))
            return new([new("PE5001", "error",
                $"Check pack '{pack.Id}' executable '{executable}' does not exist.", executable)], false);

        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = executable,
                WorkingDirectory = workspaceRoot,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                CreateNoWindow = true
            }
        };
        try
        {
            if (!process.Start())
                return new([new("PE5001", "error", $"Check pack '{pack.Id}' could not be started.", executable)], false);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new([new("PE5001", "error",
                $"Check pack '{pack.Id}' could not be started: {exception.Message}", executable)], false);
        }

        var requestJson = JsonSerializer.Serialize(request);
        using var cancellationRegistration = cancellationToken.Register(() => TryKill(process));
        await process.StandardInput.WriteAsync(requestJson.AsMemory(), cancellationToken);
        await process.StandardInput.FlushAsync(cancellationToken);
        process.StandardInput.Close();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(pack.TimeoutSeconds));
        var stdoutTask = ReadLimitedAsync(process.StandardOutput, pack.MaxOutputBytes, timeout.Token);
        var stderrTask = ReadLimitedAsync(process.StandardError, Math.Min(pack.MaxOutputBytes, 65_536), timeout.Token);
        try
        {
            await WaitForExitOrOutputFailureAsync(process, stdoutTask, stderrTask, timeout.Token);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            if (process.ExitCode != 0)
                return new([new("PE5003", "error",
                    $"Check pack '{pack.Id}' exited {process.ExitCode}: {Bound(stderr, 500)}", executable)], true);
            return ValidateResponse(pack, stdout, executable);
        }
        catch (OutputLimitException)
        {
            TryKill(process);
            return new([new("PE5004", "error",
                $"Check pack '{pack.Id}' exceeded its {pack.MaxOutputBytes}-byte output limit.", executable)], true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return new([new("PE5002", "error",
                $"Check pack '{pack.Id}' exceeded its {pack.TimeoutSeconds}-second timeout.", executable)], true);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }
    }

    private static CheckPackRunResult ValidateResponse(ConfiguredCheckPack pack, string json, string executable)
    {
        CheckPackResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<CheckPackResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException exception)
        {
            return new([new("PE5004", "error",
                $"Check pack '{pack.Id}' returned malformed JSON: {exception.Message}", executable)], true);
        }
        if (response is null || response.SchemaVersion != "1.0" ||
            !response.PackId.Equals(pack.Id, StringComparison.Ordinal) ||
            !SemanticVersion.TryParse(response.PackVersion, out var actualVersion) ||
            SemanticVersion.Parse(pack.Version).CompareTo(actualVersion) != 0)
            return new([new("PE5004", "error",
                $"Check pack '{pack.Id}' returned an incompatible protocol, pack id, or version " +
                $"(expected {pack.Version}, actual {response?.PackVersion ?? "<missing>"}).", executable)], true);
        var diagnostics = response.Diagnostics ?? [];
        var invalid = diagnostics.Where(x => !x.Code.StartsWith(pack.DiagnosticPrefix, StringComparison.Ordinal))
            .Select(x => x.Code).Distinct().Order().ToArray();
        var duplicate = diagnostics.GroupBy(x => (x.Code, x.Location, x.Message)).FirstOrDefault(x => x.Count() > 1);
        if (invalid.Length > 0 || duplicate is not null)
            return new([new("PE5005", "error",
                invalid.Length > 0
                    ? $"Check pack '{pack.Id}' emitted codes outside prefix '{pack.DiagnosticPrefix}': {string.Join(", ", invalid)}."
                    : $"Check pack '{pack.Id}' emitted duplicate diagnostic '{duplicate!.Key.Code}'.",
                executable)], true);
        return new(diagnostics.OrderBy(x => x.Code).ThenBy(x => x.Location, StringComparer.Ordinal)
            .ThenBy(x => x.Message, StringComparer.Ordinal).ToArray(), true);
    }

    private static async Task<string> ReadLimitedAsync(
        StreamReader reader,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        var builder = new StringBuilder();
        var bytes = 0;
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
                return builder.ToString();
            bytes += Encoding.UTF8.GetByteCount(buffer.AsSpan(0, read));
            if (bytes > maxBytes)
                throw new OutputLimitException();
            builder.Append(buffer, 0, read);
        }
    }

    private static async Task WaitForExitOrOutputFailureAsync(
        Process process,
        Task<string> stdout,
        Task<string> stderr,
        CancellationToken cancellationToken)
    {
        var exit = process.WaitForExitAsync(cancellationToken);
        var stdoutPending = true;
        var stderrPending = true;
        while (!exit.IsCompleted)
        {
            var candidates = new List<Task> { exit };
            if (stdoutPending)
                candidates.Add(stdout);
            if (stderrPending)
                candidates.Add(stderr);
            var completed = await Task.WhenAny(candidates);
            if (completed == stdout)
            {
                stdoutPending = false;
                await stdout;
            }
            else if (completed == stderr)
            {
                stderrPending = false;
                await stderr;
            }
        }
        await exit;
    }

    private static string Bound(string value, int limit) =>
        value.Length <= limit ? value : value[..limit] + "...";

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Failure isolation: the reported timeout/output diagnostic remains deterministic.
        }
    }

    private sealed class OutputLimitException : Exception;
}
