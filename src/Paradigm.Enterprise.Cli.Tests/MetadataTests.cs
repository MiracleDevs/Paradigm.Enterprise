using System.Reflection;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class MetadataTests
{
    #region Nested Types

    public class VisibleFixture
    {
        #region Constructors

        protected VisibleFixture()
        {
        }

        #endregion

        #region Protected Methods

        protected virtual string Resolve(int value) => value.ToString();

        #endregion
    }

    #endregion

    #region Public Methods

    [TestMethod]
    public void Resolver_de_duplicates_identical_assembly_identity_with_application_preference()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var source = typeof(MetadataTests).Assembly.Location;
            var application = Path.Combine(root, "app", Path.GetFileName(source));
            var duplicate = Path.Combine(root, "package", Path.GetFileName(source));
            Directory.CreateDirectory(Path.GetDirectoryName(application)!);
            Directory.CreateDirectory(Path.GetDirectoryName(duplicate)!);
            File.Copy(source, application);
            File.Copy(source, duplicate);
            var diagnostics = new List<Diagnostic>();
            var paths = AssetsReader.NormalizeMetadataPaths([duplicate, application], application, new Dictionary<string, (string, string)>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.GetFileNameWithoutExtension(source) }, diagnostics);
            Assert.HasCount(1, paths);
            Assert.AreEqual(application, paths[0]);
            Assert.IsEmpty(diagnostics);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void Inspector_exposes_protected_constructor_and_virtual_member()
    {
        var assembly = typeof(MetadataTests).Assembly.Location;
        var paths = TrustedPlatformAssemblies().Append(assembly).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var selection = new AssetSelection("test.csproj", "net10.0", [], paths, new Dictionary<string, (string, string)>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.GetFileNameWithoutExtension(assembly) }, assembly, assembly, false, []);
        using var inspector = new MetadataInspector(selection);
        var type = inspector.GetTypes().Single(x => x.FullName.EndsWith(".VisibleFixture", StringComparison.Ordinal));
        Assert.IsTrue(type.Members.Any(x => x.Contains("protected VisibleFixture()", StringComparison.Ordinal)));
        Assert.IsTrue(type.Members.Any(x => x.Contains("protected virtual System.String Resolve(System.Int32 value)", StringComparison.Ordinal)));
        Assert.IsEmpty(inspector.Diagnostics);
    }

    [TestMethod]
    public void Framework_selection_supports_heterogeneous_solutions()
    {
        Assert.AreEqual("net9.0", AssetsReader.SelectFramework(["net8.0", "net9.0"], "net10.0"));
        Assert.AreEqual("net10.0", AssetsReader.SelectFramework(["net9.0", "net10.0"], "net10.0"));
        Assert.Throws<AssetsException>(() => AssetsReader.SelectFramework(["net10.0"], "net9.0"));
    }

    #endregion

    #region Private Methods

    private static string[] TrustedPlatformAssemblies() => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"paradigm-metadata-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    #endregion
}