using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record AssetSelection(string Project, string Framework, IReadOnlyList<PackageInfo> Packages, IReadOnlyList<string> MetadataPaths, IReadOnlyDictionary<string, (string Name, string Version)> AssemblyOwners, IReadOnlySet<string> InspectionAssemblyNames, string? ApplicationAssembly, string? ExpectedApplicationAssembly, bool ApplicationAssemblyIsStale, IReadOnlyList<Diagnostic> Diagnostics);