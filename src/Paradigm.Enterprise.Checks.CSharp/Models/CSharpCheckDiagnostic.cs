namespace Paradigm.Enterprise.Checks.CSharp;

internal sealed record CSharpCheckDiagnostic(
    string Code,
    string Severity,
    string Message,
    string? Location);