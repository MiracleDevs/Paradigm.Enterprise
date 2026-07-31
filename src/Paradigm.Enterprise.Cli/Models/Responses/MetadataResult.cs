namespace Paradigm.Enterprise.Cli;

internal sealed record MetadataResult(IReadOnlyList<InspectedType> Types, IReadOnlyList<Diagnostic> Diagnostics);