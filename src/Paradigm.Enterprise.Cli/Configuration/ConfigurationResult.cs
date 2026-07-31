using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record ConfigurationResult(ParadigmConfiguration Configuration, IReadOnlyList<Diagnostic> Diagnostics);