using System.CodeDom.Compiler;
using System.Reflection;
using BeaconAr.Data.Receivables.Context;
using BeaconAr.Domain.Receivables.Entities;

namespace BeaconAr.Architecture.Tests;

[TestClass]
public sealed class GeneratedPersistenceTests
{
    #region Public Methods

    [TestMethod]
    public void EfptTypesCarryCompiledGeneratedCodeMetadata()
    {
        Type[] generatedDomainTypes = typeof(Product).Assembly.GetTypes()
            .Where(type => type.IsClass &&
                type.IsPublic &&
                type.Namespace == "BeaconAr.Domain.Receivables.Entities")
            .ToArray();

        Assert.HasCount(43, generatedDomainTypes);
        foreach (Type generatedType in generatedDomainTypes.Append(typeof(ReceivablesDbContext)))
        {
            GeneratedCodeAttribute? attribute = generatedType.GetCustomAttribute<GeneratedCodeAttribute>();
            Assert.IsNotNull(attribute, generatedType.FullName);
            Assert.AreEqual("EFCorePowerTools", attribute.Tool, generatedType.FullName);
            Assert.AreEqual("10.1.1386", attribute.Version, generatedType.FullName);
        }
    }

    [TestMethod]
    public void RegenerationSafetyFixturesUseTemporaryCopiesAndRedactCredentials()
    {
        string root = FindExampleRoot();
        string executable = OperatingSystem.IsWindows() ? "powershell" : "pwsh";
        var startInfo = new System.Diagnostics.ProcessStartInfo(executable)
        {
            Arguments = $"-NoProfile -NonInteractive -File \"{Path.Combine(root, "build", "regenerate-persistence.ps1")}\" -TestFailureRecovery -TestRedaction",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        using System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the EFPT safety fixtures.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.AreEqual(0, process.ExitCode, $"{standardOutput}{Environment.NewLine}{standardError}");
        StringAssert.Contains(standardOutput, "entirely within a guarded temporary copy");
        StringAssert.Contains(standardOutput, "without disclosing sentinel credentials");
    }

    #endregion

    #region Private Methods

    private static string FindExampleRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "aspire.config.json")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Beacon AR example root not found.");
    }

    #endregion
}
