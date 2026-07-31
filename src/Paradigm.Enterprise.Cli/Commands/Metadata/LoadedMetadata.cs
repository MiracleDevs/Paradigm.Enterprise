namespace Paradigm.Enterprise.Cli;

internal sealed record LoadedMetadata(ProjectSelection Selection, IReadOnlyList<PackageInfo> Packages, IReadOnlyList<InspectedType> Types, IReadOnlyList<Diagnostic> Diagnostics);