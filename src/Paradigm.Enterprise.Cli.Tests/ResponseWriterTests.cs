using System.Text.Json;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class ResponseWriterTests
{
    [TestMethod]
    public async Task Text_and_json_output_are_deterministic_and_json_has_stable_envelope()
    {
        var response = new CommandResponse(
            "1.0",
            "api search",
            "success",
            [new("Paradigm.Enterprise.Data", "1.0.33", "Sample.csproj")],
            [new("member", "Sample.Z", "protected virtual void Z()", "Paradigm.Enterprise.Data", "1.0.33")],
            []);

        var firstText = await Write(response, OutputFormat.Text);
        var secondText = await Write(response, OutputFormat.Text);
        Assert.AreEqual(firstText, secondText);
        Assert.IsFalse(firstText.Contains("\npackage ", StringComparison.Ordinal));

        var firstJson = await Write(response, OutputFormat.Json);
        var secondJson = await Write(response, OutputFormat.Json);
        Assert.AreEqual(firstJson, secondJson);
        using var json = JsonDocument.Parse(firstJson);
        CollectionAssert.AreEqual(
            new[] { "schemaVersion", "command", "status", "packages", "results", "diagnostics" },
            json.RootElement.EnumerateObject().Select(x => x.Name).ToArray());
    }

    private static async Task<string> Write(CommandResponse response, OutputFormat format)
    {
        using var output = new StringWriter();
        await ResponseWriter.WriteAsync(response, format, output);
        return output.ToString();
    }
}
