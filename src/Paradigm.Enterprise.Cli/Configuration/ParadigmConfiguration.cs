namespace Paradigm.Enterprise.Cli;

internal sealed record ParadigmConfiguration(string Path, string WorkspaceRoot, PackagePolicyConfiguration PackagePolicy, IReadOnlyList<ConfiguredSuppression> Suppressions);