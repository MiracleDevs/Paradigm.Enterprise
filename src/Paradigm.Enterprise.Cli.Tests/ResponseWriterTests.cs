using System.Text.Json;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class ResponseWriterTests
{
    #region Public Methods

    [TestMethod]
    public async Task Text_and_json_output_are_deterministic_and_json_has_stable_envelope()
    {
        var response = new CommandResponse("1.1", "api search", "success", [new("Paradigm.Enterprise.Data", "1.1.0", "Sample.csproj")], [new("member", "Sample.Z", "protected virtual void Z()", "Paradigm.Enterprise.Data", "1.1.0")], []);
        var firstText = await Write(response, OutputFormat.Text);
        var secondText = await Write(response, OutputFormat.Text);
        Assert.AreEqual(firstText, secondText);
        Assert.IsFalse(firstText.Contains("\npackage ", StringComparison.Ordinal));
        var firstJson = await Write(response, OutputFormat.Json);
        var secondJson = await Write(response, OutputFormat.Json);
        Assert.AreEqual(firstJson, secondJson);
        using var json = JsonDocument.Parse(firstJson);
        CollectionAssert.AreEqual(new[] { "schemaVersion", "command", "status", "packages", "results", "diagnostics" }, json.RootElement.EnumerateObject().Select(x => x.Name).ToArray());
    }

    [TestMethod]
    public async Task Packages_check_text_is_bounded_and_deduplicated_while_json_remains_complete()
    {
        var results = Enumerable.Range(0, 60).Select(index => new ResultItem("direct-package", $"Package.{index:D2}", "1.0.0", Project: $"Project.{index}.csproj")).Append(new("direct-package", "Package.00", "1.0.0", Project: "Duplicate.csproj")).ToArray();
        var response = new CommandResponse("1.1", "packages check", "success", [], results, []);
        var text = await Write(response, OutputFormat.Text);
        var json = await Write(response, OutputFormat.Json);
        Assert.AreEqual(50, text.Split('\n').Count(line => line.StartsWith("direct-package ", StringComparison.Ordinal)));
        StringAssert.Contains(text, "11 duplicate or additional package entries omitted");
        using var document = JsonDocument.Parse(json);
        Assert.AreEqual(61, document.RootElement.GetProperty("results").GetArrayLength());
    }

    #endregion

    #region Private Methods

    private static async Task<string> Write(CommandResponse response, OutputFormat format)
    {
        using var output = new StringWriter();
        await ResponseWriter.WriteAsync(response, format, output);
        return output.ToString();
    }

    #endregion
}