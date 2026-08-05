namespace Paradigm.Enterprise.Cli;

internal sealed record ScaffoldResult(bool DryRun, string OutputPath, IReadOnlyList<string> Files);