using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal enum OutputFormat
{
    Text,
    Json
}

internal interface ICliCommandOptions
{
    string? Project { get; }
    string? Framework { get; }
    OutputFormat Format { get; }
}

internal sealed record HelpOptions(OutputFormat Format = OutputFormat.Text) : ICliCommandOptions
{
    public string? Project => null;
    public string? Framework => null;
}

internal sealed record VersionOptions(OutputFormat Format = OutputFormat.Text) : ICliCommandOptions
{
    public string? Project => null;
    public string? Framework => null;
}

internal sealed record DoctorOptions(string? Project, OutputFormat Format) : ICliCommandOptions
{
    public string? Framework => null;
}

internal sealed record ApiSearchOptions(
    string Query,
    string? Project,
    string? Framework,
    string? Package,
    int Limit,
    OutputFormat Format) : ICliCommandOptions;

internal sealed record ApiShowOptions(
    string Symbol,
    string? Project,
    string? Framework,
    string? Package,
    OutputFormat Format) : ICliCommandOptions;

internal sealed record ApiGuideOptions(
    string Symbol,
    string? Project,
    string? Framework,
    string? Package,
    OutputFormat Format) : ICliCommandOptions;

internal sealed record InspectOptions(string? Project, string? Framework, OutputFormat Format) : ICliCommandOptions;
internal sealed record ValidateOptions(string? Project, string? Framework, OutputFormat Format) : ICliCommandOptions;
internal sealed record ChecksListOptions(string? Project, string? Config, OutputFormat Format) : ICliCommandOptions
{
    public string? Framework => null;
}

internal sealed record ChecksRunOptions(
    string? Project,
    string? Framework,
    string? Config,
    string? Pack,
    OutputFormat Format) : ICliCommandOptions;

internal sealed record PackagesCheckOptions(
    string? Project,
    string? Framework,
    string? Config,
    OutputFormat Format) : ICliCommandOptions;

internal sealed record PackagesAuditOptions(
    string? Project,
    string? Framework,
    string? Config,
    bool WarningsAsErrors,
    OutputFormat Format) : ICliCommandOptions;

internal sealed record ParsedCommand(string Name, ICliCommandOptions Options)
{
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
}

internal sealed record Diagnostic(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("location")] string? Location = null);

internal sealed record PackageInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("project")] string Project);

internal sealed record ResultItem(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("detail")] string Detail,
    [property: JsonPropertyName("package")] string? Package = null,
    [property: JsonPropertyName("version")] string? Version = null,
    [property: JsonPropertyName("project")] string? Project = null);

internal sealed record ApiAccessor(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("visibility")] string Visibility);

internal sealed record ApiGenericConstraint(
    [property: JsonPropertyName("parameter")] string Parameter,
    [property: JsonPropertyName("constraints")] IReadOnlyList<string> Constraints);

internal sealed record ApiMember(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("signature")] string Signature,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("accessors")] IReadOnlyList<ApiAccessor> Accessors,
    [property: JsonPropertyName("attributes")] IReadOnlyList<string> Attributes,
    [property: JsonPropertyName("genericConstraints")] IReadOnlyList<ApiGenericConstraint> GenericConstraints);

internal sealed record ApiTypeModel(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("namespace")] string Namespace,
    [property: JsonPropertyName("baseType")] string? BaseType,
    [property: JsonPropertyName("interfaces")] IReadOnlyList<string> Interfaces,
    [property: JsonPropertyName("attributes")] IReadOnlyList<string> Attributes,
    [property: JsonPropertyName("genericConstraints")] IReadOnlyList<ApiGenericConstraint> GenericConstraints,
    [property: JsonPropertyName("members")] IReadOnlyList<ApiMember> Members);

internal sealed record GuideInfo(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("recommendedPattern")] string RecommendedPattern,
    [property: JsonPropertyName("genericParameters")] IReadOnlyList<string> GenericParameters,
    [property: JsonPropertyName("requiredMembers")] IReadOnlyList<string> RequiredMembers,
    [property: JsonPropertyName("optionalHooks")] IReadOnlyList<string> OptionalHooks,
    [property: JsonPropertyName("discoveryAndRegistration")] IReadOnlyList<string> DiscoveryAndRegistration,
    [property: JsonPropertyName("cautions")] IReadOnlyList<string> Cautions,
    [property: JsonPropertyName("verification")] IReadOnlyList<string> Verification,
    [property: JsonPropertyName("api")] ApiTypeModel Api);

internal sealed record CheckPackInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("executable")] string Executable,
    [property: JsonPropertyName("diagnosticPrefix")] string DiagnosticPrefix);

internal sealed record CheckData(
    [property: JsonPropertyName("packs")] IReadOnlyList<CheckPackInfo> Packs,
    [property: JsonPropertyName("executed")] IReadOnlyList<string> Executed);

internal sealed record CommandResponse(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("packages")] IReadOnlyList<PackageInfo> Packages,
    [property: JsonPropertyName("results")] IReadOnlyList<ResultItem> Results,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<Diagnostic> Diagnostics,
    [property: JsonPropertyName("guide")] GuideInfo? Guide = null,
    [property: JsonPropertyName("checks")] CheckData? Checks = null);

internal sealed record ProjectSelection(IReadOnlyList<string> Projects, string DisplayPath);

internal sealed record AssetSelection(
    string Project,
    string Framework,
    IReadOnlyList<PackageInfo> Packages,
    IReadOnlyList<string> MetadataPaths,
    IReadOnlyDictionary<string, (string Name, string Version)> AssemblyOwners,
    IReadOnlySet<string> InspectionAssemblyNames,
    string? ApplicationAssembly,
    string? ExpectedApplicationAssembly,
    bool ApplicationAssemblyIsStale,
    IReadOnlyList<Diagnostic> Diagnostics);

internal sealed record InspectedAction(
    string Name,
    string Signature,
    IReadOnlyList<string> Attributes);

internal sealed record InspectedType(
    string FullName,
    string Name,
    string Namespace,
    string? BaseType,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<string> Attributes,
    IReadOnlyList<string> Members,
    IReadOnlyList<InspectedAction> Actions,
    bool IsPublic,
    bool IsAbstract,
    string AssemblyName,
    string? Package,
    string? Version,
    IReadOnlyList<ApiMember>? StructuredMembers = null,
    IReadOnlyList<ApiGenericConstraint>? GenericConstraints = null);
