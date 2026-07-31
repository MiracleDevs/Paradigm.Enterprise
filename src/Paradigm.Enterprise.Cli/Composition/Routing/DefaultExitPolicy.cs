namespace Paradigm.Enterprise.Cli;

internal sealed class DefaultExitPolicy : IExitPolicy
{
    #region Public Methods

    public int GetExitCode(CommandResponse response)
    {
        if (response.Diagnostics.Any(x => x.Code == "PE1002" && x.Severity == "error"))
            return 3;
        return response.Diagnostics.Any(x => x.Severity == "error") ? 1 : 0;
    }

    #endregion
}