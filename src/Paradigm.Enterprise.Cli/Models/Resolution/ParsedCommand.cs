using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record ParsedCommand(string Name, ICliCommandOptions Options)
{
#region Properties
    public string? Query => Options switch
    {
        ApiSearchOptions options => options.Query,
        ApiShowOptions options => options.Symbol,
        ApiGuideOptions options => options.Symbol,
        _ => null
    };
    public string? Project => Options.Project;
    public string? Framework => Options.Framework;
    public string? Package => Options switch
    {
        ApiSearchOptions options => options.Package,
        ApiShowOptions options => options.Package,
        ApiGuideOptions options => options.Package,
        _ => null
    };
    public int Limit => Options is ApiSearchOptions options ? options.Limit : 20;
    public OutputFormat Format => Options.Format;
#endregion
}
