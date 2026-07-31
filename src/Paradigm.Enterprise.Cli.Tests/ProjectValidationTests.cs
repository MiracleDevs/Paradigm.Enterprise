namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class ProjectValidationTests
{
    #region Public Methods

    [TestMethod]
    public void Lower_layer_reference_to_host_is_an_error()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"paradigm-layers-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var domain = Path.Combine(directory, "Sample.Domain.csproj");
            File.WriteAllText(domain, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <ProjectReference Include="Sample.WebApi.csproj" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(directory, "Sample.WebApi.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            var selection = new ProjectSelection([domain], domain);
            var diagnostic = Analysis.ValidateLayers(selection).Single();
            Assert.AreEqual("PE2001", diagnostic.Code);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    #endregion
}