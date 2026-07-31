namespace Paradigm.Enterprise.Cli;
internal sealed class MetadataService : IMetadataService
{
#region Public Methods
    public MetadataResult Inspect(AssetSelection selection)
    {
        using var inspector = new MetadataInspector(selection);
        var types = inspector.GetTypes();
        return new(types, inspector.Diagnostics);
    }
#endregion
}
