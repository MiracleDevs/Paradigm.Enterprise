namespace Paradigm.Enterprise.Cli;
internal interface IMetadataService
{
    MetadataResult Inspect(AssetSelection selection);
}
