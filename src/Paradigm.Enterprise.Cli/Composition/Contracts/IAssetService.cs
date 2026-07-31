namespace Paradigm.Enterprise.Cli;
internal interface IAssetService
{
    AssetSelection Read(string project, string? framework, bool packagesOnly = false);
}
