using System.Diagnostics;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class CheckPackProcessTests
{
    [TestMethod]
    [DataRow("nonzero", 1_048_576, "PE5003")]
    [DataRow("malformed", 1_048_576, "PE5004")]
    [DataRow("oversized", 1024, "PE5004")]
    [DataRow("protocol", 1_048_576, "PE5004")]
    [DataRow("version-mismatch", 1_048_576, "PE5004")]
    [DataRow("wrong-prefix", 1_048_576, "PE5005")]
    [DataRow("duplicate", 1_048_576, "PE5005")]
    public async Task Pack_protocol_failures_are_isolated(
        string packId,
        int outputLimit,
        string expectedCode)
    {
        using var workspace = FixtureWorkspace.Create();
        var pack = workspace.Pack(packId, timeoutSeconds: 10, maxOutputBytes: outputLimit);

        var result = await new CheckPackRunner().RunAsync(
            pack, workspace.Root, Request(packId), CancellationToken.None);

        Assert.IsTrue(result.Executed);
        Assert.AreEqual(expectedCode, result.Diagnostics.Single().Code);
    }

    [TestMethod]
    public async Task Valid_pack_diagnostics_are_deterministically_ordered()
    {
        using var workspace = FixtureWorkspace.Create();
        var pack = workspace.Pack("success");

        var result = await new CheckPackRunner().RunAsync(
            pack, workspace.Root, Request("success"), CancellationToken.None);

        CollectionAssert.AreEqual(
            new[] { "PX001", "PX002" },
            result.Diagnostics.Select(x => x.Code).ToArray());
    }

    [TestMethod]
    public async Task Pack_timeout_kills_the_process_tree()
    {
        using var workspace = FixtureWorkspace.Create();
        var pack = workspace.Pack("timeout", timeoutSeconds: 1);

        var result = await new CheckPackRunner().RunAsync(
            pack, workspace.Root, Request("timeout"), CancellationToken.None);
        var pid = int.Parse(await File.ReadAllTextAsync(
            Path.Combine(workspace.Root, "check-fixture.pid")));

        Assert.AreEqual("PE5002", result.Diagnostics.Single().Code);
        await AssertProcessStops(pid);
    }

    [TestMethod]
    public async Task Very_large_pipe_output_is_killed_promptly_as_PE5004()
    {
        using var workspace = FixtureWorkspace.Create();
        var pack = workspace.Pack("oversized-pipe", timeoutSeconds: 20, maxOutputBytes: 1024);
        var started = Stopwatch.StartNew();

        var result = await new CheckPackRunner().RunAsync(
            pack, workspace.Root, Request("oversized-pipe"), CancellationToken.None);
        var pid = int.Parse(await File.ReadAllTextAsync(
            Path.Combine(workspace.Root, "check-fixture.pid")));

        Assert.AreEqual("PE5004", result.Diagnostics.Single().Code);
        Assert.IsTrue(started.Elapsed < TimeSpan.FromSeconds(5),
            $"Output limiting took {started.Elapsed} instead of terminating promptly.");
        await AssertProcessStops(pid);
    }

    [TestMethod]
    public async Task Build_metadata_is_allowed_in_actual_pack_version()
    {
        using var workspace = FixtureWorkspace.Create();
        var pack = workspace.Pack("actual-version");

        var result = await new CheckPackRunner().RunAsync(
            pack with { Version = "1.1.0" }, workspace.Root,
            Request("actual-version", "1.1.0"), CancellationToken.None);

        Assert.IsEmpty(result.Diagnostics);
    }

    [TestMethod]
    public async Task Sibling_prefix_path_is_rejected_by_canonical_containment()
    {
        using var workspace = FixtureWorkspace.Create();
        using var outside = FixtureWorkspace.Create();
        var relative = Path.GetRelativePath(
            workspace.Root,
            outside.Executable);
        var pack = new ConfiguredCheckPack(
            "escape", "1.0.0", relative, "PX", 10, 1_048_576);

        var result = await new CheckPackRunner().RunAsync(
            pack, workspace.Root, Request("escape"), CancellationToken.None);

        Assert.IsFalse(result.Executed);
        Assert.AreEqual("PE5001", result.Diagnostics.Single().Code);
    }

    [TestMethod]
    public async Task Symlink_to_executable_outside_workspace_is_rejected()
    {
        using var workspace = FixtureWorkspace.Create();
        using var outside = FixtureWorkspace.Create();
        var link = Path.Combine(workspace.Root, "linked-tools");
        try
        {
            Directory.CreateSymbolicLink(link, Path.GetDirectoryName(outside.Executable)!);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or
                                              PlatformNotSupportedException)
        {
            Assert.Inconclusive($"Symbolic links are unavailable in this environment: {exception.Message}");
        }
        var pack = new ConfiguredCheckPack(
            "symlink", "1.0.0",
            Path.Combine("linked-tools", Path.GetFileName(outside.Executable)),
            "PX", 10, 1_048_576);

        var result = await new CheckPackRunner().RunAsync(
            pack, workspace.Root, Request("symlink"), CancellationToken.None);

        Assert.IsFalse(result.Executed);
        Assert.AreEqual("PE5001", result.Diagnostics.Single().Code);
    }

    [TestMethod]
    public async Task Caller_cancellation_kills_the_check_pack_process_tree()
    {
        using var workspace = FixtureWorkspace.Create();
        var pack = workspace.Pack("cancel", timeoutSeconds: 30);
        using var cancellation = new CancellationTokenSource();
        var run = new CheckPackRunner().RunAsync(
            pack, workspace.Root, Request("cancel"), cancellation.Token);
        var pidPath = Path.Combine(workspace.Root, "check-fixture.pid");
        await WaitForFile(pidPath);
        var pid = int.Parse(await File.ReadAllTextAsync(pidPath));

        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => run);
        await AssertProcessStops(pid);
    }

    [TestMethod]
    public async Task Caller_cancellation_kills_the_package_audit_process_tree()
    {
        using var workspace = FixtureWorkspace.Create();
        var project = Path.Combine(workspace.Root, "Audit.csproj");
        await File.WriteAllTextAsync(project, "<Project />");
        using var cancellation = new CancellationTokenSource();
        var run = new DotNetPackageAuditor(workspace.Executable).RunAsync(
            project, "--outdated", false, cancellation.Token);
        var pidPath = Path.Combine(workspace.Root, "audit-fixture.pid");
        await WaitForFile(pidPath);
        var pid = int.Parse(await File.ReadAllTextAsync(pidPath));

        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => run);
        await AssertProcessStops(pid);
    }

    private static CheckPackRequest Request(string packId, string version = "1.0.0") =>
        new("1.0", packId, version, [], [], new Dictionary<string, string>(), []);

    private static async Task WaitForFile(string path)
    {
        for (var attempt = 0; attempt < 200 && !File.Exists(path); attempt++)
            await Task.Delay(25);
        Assert.IsTrue(File.Exists(path), $"Fixture process did not create '{path}'.");
    }

    private static async Task AssertProcessStops(int processId)
    {
        for (var attempt = 0; attempt < 200 && IsRunning(processId); attempt++)
            await Task.Delay(25);
        Assert.IsFalse(IsRunning(processId), $"Process {processId} remained alive after cancellation.");
    }

    private static bool IsRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private sealed class FixtureWorkspace : IDisposable
    {
        private FixtureWorkspace(string root, string executable, string executableRelative)
        {
            Root = root;
            Executable = executable;
            ExecutableRelative = executableRelative;
        }

        public string Root { get; }

        public string Executable { get; }

        private string ExecutableRelative { get; }

        public static FixtureWorkspace Create()
        {
            var root = Path.Combine(Path.GetTempPath(), $"paradigm pack fixture {Guid.NewGuid():N}");
            var tools = Path.Combine(root, "tools");
            Directory.CreateDirectory(tools);
            var repository = FindRepositoryRoot();
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            var source = Path.Combine(repository, "src", "Paradigm.Enterprise.Cli.Tests", "Fixtures",
                "CheckPackFixture", "bin", configuration, "net10.0");
            foreach (var file in Directory.EnumerateFiles(source))
                File.Copy(file, Path.Combine(tools, Path.GetFileName(file)));
            var fileName = OperatingSystem.IsWindows() ? "CheckPackFixture.exe" : "CheckPackFixture";
            var executable = Path.Combine(tools, fileName);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(executable,
                    File.GetUnixFileMode(executable) | UnixFileMode.UserExecute);
            return new(root, executable, Path.Combine("tools", fileName));
        }

        public ConfiguredCheckPack Pack(
            string id,
            int timeoutSeconds = 10,
            int maxOutputBytes = 1_048_576) =>
            new(id, "1.0.0", ExecutableRelative, "PX", timeoutSeconds, maxOutputBytes);

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
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
