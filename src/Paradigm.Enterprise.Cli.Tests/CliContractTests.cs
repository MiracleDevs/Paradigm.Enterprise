using System.Text.Json;

namespace Paradigm.Enterprise.Cli.Tests;
[TestClass]
public class CliContractTests
{
#region Public Methods
    [TestMethod]
    public async Task Help_text_and_exit_match_the_current_contract()
    {
        using var output = new StringWriter();
        var exit = await TestCliApplication.RunAsync([], output, TextWriter.Null);
        Assert.AreEqual(0, exit);
        Assert.AreEqual(Normalize(CommandLine.Usage + Environment.NewLine), Normalize(output.ToString()));
    }

    [TestMethod]
    public async Task Resolution_json_shape_content_and_exit_match_the_current_contract()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"paradigm-contract-missing-{Guid.NewGuid():N}");
        using var output = new StringWriter();
        var exit = await TestCliApplication.RunAsync(["inspect", "--project", missing, "--format", "json"], output, TextWriter.Null);
        Assert.AreEqual(3, exit);
        using var document = JsonDocument.Parse(output.ToString());
        var root = document.RootElement;
        Assert.AreEqual("1.1", root.GetProperty("schemaVersion").GetString());
        Assert.AreEqual("inspect", root.GetProperty("command").GetString());
        Assert.AreEqual("error", root.GetProperty("status").GetString());
        Assert.AreEqual(0, root.GetProperty("results").GetArrayLength());
        var diagnostic = root.GetProperty("diagnostics")[0];
        Assert.AreEqual("PE1002", diagnostic.GetProperty("code").GetString());
        Assert.AreEqual("error", diagnostic.GetProperty("severity").GetString());
        Assert.AreEqual($"Project path '{Path.GetFullPath(missing)}' does not exist.", diagnostic.GetProperty("message").GetString());
    }

    [TestMethod]
    public async Task Version_text_json_and_exits_match_the_current_contract()
    {
        using var text = new StringWriter();
        using var json = new StringWriter();
        var textExit = await TestCliApplication.RunAsync(["--version"], text, TextWriter.Null);
        var jsonExit = await TestCliApplication.RunAsync(["--version", "--format", "json"], json, TextWriter.Null);
        Assert.AreEqual(0, textExit);
        Assert.AreEqual("1.1.0" + Environment.NewLine, text.ToString());
        Assert.AreEqual(2, jsonExit);
        Assert.AreEqual("", json.ToString());
    }

    [TestMethod]
    public async Task Invalid_command_error_text_and_exit_match_the_current_contract()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = await TestCliApplication.RunAsync(["unknown-command"], output, error);
        Assert.AreEqual(2, exit);
        Assert.AreEqual("", output.ToString());
        Assert.AreEqual(Normalize($"Unknown command 'unknown-command'.{Environment.NewLine}" + CommandLine.Usage + Environment.NewLine), Normalize(error.ToString()));
    }

    [TestMethod]
    public async Task Missing_project_text_diagnostic_and_exit_match_the_current_contract()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"paradigm-contract-missing-{Guid.NewGuid():N}");
        using var output = new StringWriter();
        var exit = await TestCliApplication.RunAsync(["inspect", "--project", missing], output, TextWriter.Null);
        Assert.AreEqual(3, exit);
        Assert.AreEqual(Normalize($"inspect: error{Environment.NewLine}" + $"PE1002 error: Project path '{Path.GetFullPath(missing)}' does not exist.{Environment.NewLine}"), Normalize(output.ToString()));
    }

#endregion
#region Private Methods
    private static string Normalize(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal);
#endregion
}
