namespace Paradigm.Enterprise.Cli;
internal interface IExitPolicy
{
    int GetExitCode(CommandResponse response);
}
