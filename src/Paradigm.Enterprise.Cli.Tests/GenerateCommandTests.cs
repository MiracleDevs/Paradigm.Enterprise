namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class GenerateCommandTests
{
    #region Public Methods

    [TestMethod]
    public async Task Json_generation_uses_separate_assembly_and_source_output_paths()
    {
        var repository = FindRepositoryRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var assembly = Path.Combine(repository, "src", "Paradigm.Enterprise.Cli.Tests", "Fixtures", "GoodPractices", "bin", configuration, "net10.0", "GoodPractices.dll");
        var output = Path.Combine(Path.GetTempPath(), $"paradigm-generation-{Guid.NewGuid():N}");
        try
        {
            using var writer = new StringWriter();
            var exit = await TestCliApplication.RunAsync(["generate", "json", "--project-name", "GoodPractices", "--assembly", assembly, "--output", output, "--format", "json"], writer, TextWriter.Null);
            Assert.AreEqual(0, exit, writer.ToString());
            Assert.IsTrue(Directory.Exists(Path.Combine(output, "JsonSerializerContexts")));
        }
        finally
        {
            if (Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    #endregion

    #region Private Methods

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "src", "Paradigm.Enterprise.slnx")))
                return current.FullName;
        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    #endregion
}