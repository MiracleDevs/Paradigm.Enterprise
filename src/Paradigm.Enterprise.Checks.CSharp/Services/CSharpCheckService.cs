namespace Paradigm.Enterprise.Checks.CSharp;

internal static class CSharpCheckService
{
    #region Public Methods

    public static IReadOnlyList<CSharpCheckDiagnostic> Analyze(CSharpCheckProject project) =>
        CSharpChecks.Analyze(project);

    #endregion
}