namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class CommandLineTests
{
    [TestMethod]
    public void Search_defaults_to_bounded_text_output()
    {
        var valid = CommandLine.TryParse(["api", "search", "repository"], out var command, out _);

        Assert.IsTrue(valid);
        Assert.AreEqual("api search", command!.Name);
        Assert.AreEqual(20, command.Limit);
        Assert.AreEqual(OutputFormat.Text, command.Format);
    }

    [TestMethod]
    public void Search_accepts_all_options()
    {
        var valid = CommandLine.TryParse(
            ["api", "search", "provider", "--project", "app.slnx", "--framework", "net10.0", "--package", "Providers", "--limit", "7", "--format", "json"],
            out var command,
            out _);

        Assert.IsTrue(valid);
        Assert.AreEqual(7, command!.Limit);
        Assert.AreEqual("Providers", command.Package);
        Assert.AreEqual(OutputFormat.Json, command.Format);
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("101")]
    [DataRow("many")]
    public void Search_rejects_invalid_limits(string value)
    {
        Assert.IsFalse(CommandLine.TryParse(["api", "search", "provider", "--limit", value], out _, out _));
    }

    [TestMethod]
    public async Task Invalid_arguments_return_two()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["unknown"], output, error);

        Assert.AreEqual(2, exitCode);
        StringAssert.Contains(error.ToString(), "Unknown command");
    }

    [TestMethod]
    public async Task Version_is_available_without_a_project()
    {
        using var output = new StringWriter();
        var exitCode = await CliApplication.RunAsync(["--version"], output, TextWriter.Null);

        Assert.AreEqual(0, exitCode);
        StringAssert.StartsWith(output.ToString(), "1.0.33");
    }

    [TestMethod]
    public async Task Resolution_failure_has_stable_json_and_exit_three()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"paradigm-missing-{Guid.NewGuid():N}");
        using var output = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", missing, "--format", "json"],
            output,
            TextWriter.Null);

        Assert.AreEqual(3, exitCode);
        using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
        var root = json.RootElement;
        Assert.AreEqual("1.0", root.GetProperty("schemaVersion").GetString());
        Assert.AreEqual("inspect", root.GetProperty("command").GetString());
        Assert.AreEqual("error", root.GetProperty("status").GetString());
        Assert.AreEqual("PE1002", root.GetProperty("diagnostics")[0].GetProperty("code").GetString());
        Assert.IsTrue(root.TryGetProperty("packages", out _));
        Assert.IsTrue(root.TryGetProperty("results", out _));
    }

    [TestMethod]
    public async Task Parsed_selection_failure_preserves_json_shape_and_exit_two()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"paradigm-solutions-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "One.slnx"), "<Solution />");
            File.WriteAllText(Path.Combine(directory, "Two.slnx"), "<Solution />");
            using var output = new StringWriter();

            var exit = await CliApplication.RunAsync(
                ["inspect", "--project", directory, "--format", "json"],
                output,
                TextWriter.Null);

            Assert.AreEqual(2, exit);
            using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
            Assert.AreEqual("PE0002", json.RootElement.GetProperty("diagnostics")[0].GetProperty("code").GetString());
            Assert.IsTrue(json.RootElement.TryGetProperty("packages", out _));
            Assert.IsTrue(json.RootElement.TryGetProperty("results", out _));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task Protected_framework_extension_points_are_searchable()
    {
        var root = FindRepositoryRoot();
        var dataProject = Path.Combine(root, "src", "Paradigm.Enterprise.Data", "Paradigm.Enterprise.Data.csproj");
        var providerProject = Path.Combine(root, "src", "Paradigm.Enterprise.Providers", "Paradigm.Enterprise.Providers.csproj");

        var editRepository = await Run(["api", "show", "EditRepositoryBase", "--project", dataProject]);
        var search = await Run(["api", "search", "GetSearchPaginatedFunction", "--project", dataProject]);
        var provider = await Run(["api", "show", "ProviderBase", "--project", providerProject]);

        Assert.AreEqual(0, editRepository.Exit);
        StringAssert.Contains(editRepository.Output, "protected System.Void RemoveAggregate");
        StringAssert.Contains(editRepository.Output, "protected virtual System.Void DeleteRemovedAggregates");
        Assert.AreEqual(0, search.Exit);
        StringAssert.Contains(search.Output, "protected virtual");
        Assert.AreEqual(0, provider.Exit);
        StringAssert.Contains(provider.Output, "protected virtual TProvider GetProvider");
        Assert.IsFalse(provider.Output.Contains("\npackage ", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Directory_with_multiple_solutions_requires_explicit_selection()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"paradigm-solutions-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "One.slnx"), "<Solution />");
            File.WriteAllText(Path.Combine(directory, "Two.slnx"), "<Solution />");

            var exception = Assert.Throws<ArgumentException>(() => ProjectResolver.Resolve(directory));

            StringAssert.Contains(exception.Message, "Multiple solutions");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<(int Exit, string Output)> Run(string[] args)
    {
        using var output = new StringWriter();
        var exit = await CliApplication.RunAsync(args, output, TextWriter.Null);
        return (exit, output.ToString());
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "src", "Paradigm.Enterprise.slnx")))
                return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
