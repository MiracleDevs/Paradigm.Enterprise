namespace Paradigm.Enterprise.Cli;
internal sealed class AssetService : IAssetService
{
#region Public Methods
    public AssetSelection Read(string project, string? framework, bool packagesOnly = false) => AssetsReader.Read(project, framework, packagesOnly);
#endregion
}
