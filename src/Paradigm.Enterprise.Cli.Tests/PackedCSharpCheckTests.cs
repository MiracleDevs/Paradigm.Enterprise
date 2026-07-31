using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class PackedCSharpCheckTests
{
    [TestMethod]
    [Timeout(120_000)]
    public async Task Packed_csharp_tool_reports_semantic_violations_from_evaluated_compile_items()
    {
        var repository = FindRepositoryRoot();
        var temporary = Path.Combine(Path.GetTempPath(), $"paradigm-packed-check-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporary);
        try
        {
            var packages = Path.Combine(temporary, "packages");
            var tools = Path.Combine(temporary, "tools");
            Directory.CreateDirectory(packages);
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            var checkProject = Path.Combine(repository, "src", "Paradigm.Enterprise.Checks.CSharp",
                "Paradigm.Enterprise.Checks.CSharp.csproj");
            var fixtureProject = Path.Combine(repository, "src", "Paradigm.Enterprise.Cli.Tests", "Fixtures",
                "SemanticViolations", "SemanticViolations.csproj");
            var incompleteProject = Path.Combine(repository, "src", "Paradigm.Enterprise.Cli.Tests", "Fixtures",
                "SemanticIncomplete", "SemanticIncomplete.csproj");
            var version = CentralVersion(repository);
            var integrationVersion = $"{version}-integration-{Guid.NewGuid():N}";

            var pack = await Run("dotnet",
                ["pack", checkProject, "--configuration", configuration, "--no-build", "--output", packages,
                    $"-p:PackageVersion={integrationVersion}"],
                null, repository);
            Assert.AreEqual(0, pack.ExitCode, pack.Error);
            var install = await Run("dotnet",
                ["tool", "install", "--tool-path", tools, "--add-source", packages,
                    "--ignore-failed-sources", "--no-cache", "--version", integrationVersion,
                    "Paradigm.Enterprise.Checks.CSharp"],
                null, repository);
            Assert.AreEqual(0, install.ExitCode, install.Error);
            var restoreIncomplete = await Run(
                "dotnet", ["restore", incompleteProject], null, repository);
            Assert.AreEqual(0, restoreIncomplete.ExitCode, restoreIncomplete.Error);

            var request = JsonSerializer.Serialize(new
            {
                schemaVersion = "1.0",
                packId = "csharp",
                packVersion = version,
                projects = new[] { new { path = fixtureProject, framework = "net10.0" } },
                packages = Array.Empty<object>(),
                metadata = new Dictionary<string, string>(),
                suppressions = Array.Empty<object>()
            });
            var executable = Path.Combine(tools,
                OperatingSystem.IsWindows() ? "paradigm-checks-csharp.exe" : "paradigm-checks-csharp");
            var result = await Run(executable, [], request, repository);

            Assert.AreEqual(0, result.ExitCode, result.Error);
            using var response = JsonDocument.Parse(result.Output);
            var diagnostics = response.RootElement.GetProperty("diagnostics").EnumerateArray().ToArray();
            CollectionAssert.AreEqual(
                new[] { "PE3103", "PE3103", "PE3103", "PE3104", "PE3104" },
                diagnostics.Select(x => x.GetProperty("code").GetString()).ToArray());

            var paginationLocations = diagnostics.Where(x =>
                    x.GetProperty("code").GetString() == "PE3103")
                .Select(x => x.GetProperty("location").GetString()!)
                .ToArray();
            CollectionAssert.Contains(paginationLocations, ExpectedLocation(
                Path.Combine(Path.GetDirectoryName(fixtureProject)!, "Violations.cs"),
                "var rows = await query",
                "query"));
            CollectionAssert.Contains(paginationLocations, ExpectedLocation(
                Path.Combine(Path.GetDirectoryName(fixtureProject)!, "AllowedCases.cs"),
                "return await context.Orders.Skip(1)",
                "context.Orders.Skip(1)"));
            CollectionAssert.Contains(paginationLocations, ExpectedLocation(
                Path.Combine(Path.GetDirectoryName(fixtureProject)!, "AllowedCases.cs"),
                "return await context.Orders.Skip(2)",
                "context.Orders.Skip(2)"));
            Assert.IsFalse(diagnostics.Where(x =>
                    x.GetProperty("code").GetString() == "PE3103")
                .Any(x => x.GetProperty("message").GetString()!
                    .Contains("SearchRealStoredProcedure", StringComparison.Ordinal)));
            var mutationLocations = diagnostics.Where(x =>
                    x.GetProperty("code").GetString() == "PE3104")
                .Select(x => x.GetProperty("location").GetString()!)
                .ToArray();
            CollectionAssert.Contains(mutationLocations, ExpectedLocation(
                Path.Combine(Path.GetDirectoryName(fixtureProject)!, "Violations.cs"),
                "order.Status = \"active\"",
                "order.Status"));
            CollectionAssert.Contains(mutationLocations, ExpectedLocation(
                Path.Combine(Path.GetDirectoryName(fixtureProject)!, "..", "SemanticLinked", "LinkedViolation.cs"),
                "order.Status = \"linked\"",
                "order.Status"));
            Assert.IsFalse(diagnostics.Any(x =>
                x.GetProperty("location").GetString()!.Contains("Excluded.cs", StringComparison.Ordinal)));

            var mismatchedRequest = JsonSerializer.Serialize(new
            {
                schemaVersion = "1.0",
                packId = "csharp",
                packVersion = "9.9.9",
                projects = Array.Empty<object>(),
                suppressions = Array.Empty<object>()
            });
            var mismatch = await Run(executable, [], mismatchedRequest, repository);
            Assert.AreEqual(1, mismatch.ExitCode);
            StringAssert.Contains(
                mismatch.Error,
                "Configured pack version '9.9.9' does not match tool version '1.1.0");

            var incompleteRequest = JsonSerializer.Serialize(new
            {
                schemaVersion = "1.0",
                packId = "csharp",
                packVersion = version,
                projects = new[] { new { path = incompleteProject, framework = "net10.0" } },
                suppressions = Array.Empty<object>()
            });
            var incomplete = await Run(executable, [], incompleteRequest, repository);
            Assert.AreEqual(1, incomplete.ExitCode);
            StringAssert.Contains(incomplete.Error, "Semantic compilation");
            StringAssert.Contains(incomplete.Error, "CS0246");
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }

    private static string ExpectedLocation(string path, string lineFragment, string nodeFragment)
    {
        path = Path.GetFullPath(path);
        var lines = File.ReadAllLines(path);
        var lineIndex = Array.FindIndex(lines,
            line => line.Contains(lineFragment, StringComparison.Ordinal));
        Assert.IsTrue(lineIndex >= 0, $"Could not find '{lineFragment}' in '{path}'.");
        var column = lines[lineIndex].IndexOf(nodeFragment, StringComparison.Ordinal) + 1;
        return $"{path}({lineIndex + 1},{column})";
    }

    private static string CentralVersion(string repository)
    {
        var text = File.ReadAllText(Path.Combine(repository, "build", "Paradigm.Version.props"));
        const string start = "<ParadigmEnterpriseVersion>";
        var index = text.IndexOf(start, StringComparison.Ordinal) + start.Length;
        return text[index..text.IndexOf("</ParadigmEnterpriseVersion>", index, StringComparison.Ordinal)];
    }

    private static async Task<(int ExitCode, string Output, string Error)> Run(
        string executable,
        IReadOnlyList<string> arguments,
        string? input,
        string workingDirectory)
    {
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = executable,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
                CreateNoWindow = true
            }
        };
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);
        process.Start();
        if (input is not null)
            await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await output, await error);
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory);
             current is not null;
             current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "src", "Paradigm.Enterprise.slnx")))
                return current.FullName;
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
