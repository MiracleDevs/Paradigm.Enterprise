namespace Paradigm.Enterprise.Cli;

internal sealed record ApiMatch(IReadOnlyList<ResultItem> Results, IReadOnlyList<InspectedType> Types, IReadOnlyList<Diagnostic> Diagnostics);