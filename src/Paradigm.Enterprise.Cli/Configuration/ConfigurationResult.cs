namespace Paradigm.Enterprise.Cli;

internal sealed record ConfigurationResult(ParadigmConfiguration Configuration, IReadOnlyList<Diagnostic> Diagnostics);