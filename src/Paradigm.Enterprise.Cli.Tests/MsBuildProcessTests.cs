using Paradigm.Enterprise.Checks.CSharp;
using System.Diagnostics;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
[TestCategory("Integration")]
public class MsBuildProcessTests
{
    #region Public Methods

    [TestMethod]
    public void Evaluation_captures_output_and_disables_persistent_build_workers()
    {
        var output = MsBuildProcess.Run(["ProcessOutput.csproj"], FixtureExecutable());
        Assert.AreEqual("evaluation output" + Environment.NewLine, output);
    }

    [TestMethod]
    public void Failed_evaluation_reports_the_command_exit_code_and_both_streams()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MsBuildProcess.Run(["ProcessFailure.csproj"], FixtureExecutable()));
        StringAssert.Contains(exception.Message, "code 7");
        StringAssert.Contains(exception.Message, "ProcessFailure.csproj");
        StringAssert.Contains(exception.Message, "evaluation output");
        StringAssert.Contains(exception.Message, "evaluation diagnostic");
    }

    [TestMethod]
    [DataRow("ProcessTimeout.csproj")]
    [DataRow("ProcessInheritedOutput.csproj")]
    public void Timeout_bounds_execution_and_inherited_output_pipes(string project)
    {
        var elapsed = Stopwatch.StartNew();
        var exception = Assert.Throws<TimeoutException>(() =>
            MsBuildProcess.Run([project], FixtureExecutable(), TimeSpan.FromSeconds(2)));
        Assert.IsLessThan(TimeSpan.FromSeconds(5), elapsed.Elapsed);
        StringAssert.Contains(exception.Message, "timed out after 2 seconds");
        StringAssert.Contains(exception.Message, project);
        StringAssert.Contains(exception.Message, "evaluation output");
        StringAssert.Contains(exception.Message, "evaluation diagnostic");
    }

    #endregion

    #region Private Methods

    private static string FixtureExecutable()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "src", "Paradigm.Enterprise.slnx")))
                return Path.Combine(current.FullName, "src", "Paradigm.Enterprise.Cli.Tests", "Fixtures", "DotNetAuditFixture", "bin",
                    new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name, "net10.0",
                    OperatingSystem.IsWindows() ? "DotNetAuditFixture.exe" : "DotNetAuditFixture");
        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    #endregion
}
