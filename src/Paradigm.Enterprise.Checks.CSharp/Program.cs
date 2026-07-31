using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

try
{
    var requestJson = await Console.In.ReadToEndAsync();
    var request = JsonSerializer.Deserialize<CheckRequest>(requestJson, jsonOptions)
                  ?? throw new InvalidOperationException("The check request was empty.");
    if (request.SchemaVersion != "1.0")
        throw new InvalidOperationException($"Unsupported protocol '{request.SchemaVersion}'.");
    var toolVersion = ToolVersion();
    if (!VersionMatches(request.PackVersion, toolVersion))
        throw new InvalidOperationException(
            $"Configured pack version '{request.PackVersion}' does not match tool version '{toolVersion}'.");
    var diagnostics = new List<CheckDiagnostic>();
    foreach (var project in request.Projects.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase))
        diagnostics.AddRange(CSharpChecks.Analyze(project));
    diagnostics = diagnostics
        .Where(x => !SuppressionPolicy.IsSuppressed(x, request.Suppressions))
        .DistinctBy(x => (x.Code, x.Location, x.Message))
        .OrderBy(x => x.Code, StringComparer.Ordinal)
        .ThenBy(x => x.Location, StringComparer.Ordinal)
        .ThenBy(x => x.Message, StringComparer.Ordinal)
        .ToList();
    await Console.Out.WriteAsync(JsonSerializer.Serialize(
        new CheckResponse("1.0", request.PackId, toolVersion, diagnostics), jsonOptions));
    return 0;
}
catch (Exception exception)
{
    await Console.Error.WriteAsync(exception.ToString());
    return 1;
}

static string ToolVersion() =>
    System.Reflection.Assembly.GetEntryAssembly()?
        .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
        .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
        .SingleOrDefault()?.InformationalVersion ?? "0.0.0";

static bool VersionMatches(string expected, string actual) =>
    NuGetVersionParts(expected) == NuGetVersionParts(actual);

static string NuGetVersionParts(string value) => value.Split('+')[0];

internal static class CSharpChecks
{
    public static IReadOnlyList<CheckDiagnostic> Analyze(CheckProject project)
    {
        var evaluated = EvaluatedProject.Load(project.Path, project.Framework);
        var parseOptions = new CSharpParseOptions(evaluated.LanguageVersion,
            preprocessorSymbols: evaluated.DefineConstants);
        var trees = evaluated.SourceFiles.Select(path =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(path),
                parseOptions, path)).ToArray();
        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(project.Path),
            trees,
            evaluated.ReferencePaths.Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(evaluated.OutputKind,
                nullableContextOptions: evaluated.NullableContextOptions,
                allowUnsafe: evaluated.AllowUnsafe));
        var generators = LoadGenerators(evaluated.AnalyzerPaths);
        if (generators.Count > 0)
        {
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(
                compilation, out var generatedCompilation, out _);
            compilation = (CSharpCompilation)generatedCompilation;
        }
        var compilationErrors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .OrderBy(diagnostic => diagnostic.Location.GetLineSpan().Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ThenBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ToArray();
        if (compilationErrors.Length > 0)
            throw new InvalidOperationException(
                $"Semantic compilation for '{project.Path}' ({project.Framework}) is incomplete: " +
                string.Join(" | ", compilationErrors.Take(10).Select(x => x.ToString())) +
                (compilationErrors.Length > 10 ? $" | ... {compilationErrors.Length - 10} more error(s)" : ""));
        var diagnostics = new List<CheckDiagnostic>();
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            diagnostics.AddRange(PaginatedEfQueries(tree, model));
            diagnostics.AddRange(ExternalEntityMutation(tree, model));
        }
        return diagnostics;
    }

    private static IReadOnlyList<ISourceGenerator> LoadGenerators(IEnumerable<string> analyzerPaths)
    {
        var generators = new List<ISourceGenerator>();
        foreach (var path in analyzerPaths)
        {
            Assembly assembly;
            try
            {
                var identity = AssemblyName.GetAssemblyName(path);
                assembly = AppDomain.CurrentDomain.GetAssemblies()
                               .FirstOrDefault(candidate =>
                                   AssemblyName.ReferenceMatchesDefinition(
                                       candidate.GetName(), identity))
                           ?? Assembly.LoadFrom(path);
            }
            catch (BadImageFormatException)
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
            }

            foreach (var type in types.Where(type =>
                         !type.IsAbstract &&
                         (typeof(ISourceGenerator).IsAssignableFrom(type) ||
                          typeof(IIncrementalGenerator).IsAssignableFrom(type))))
            {
                if (Activator.CreateInstance(type, nonPublic: true) is ISourceGenerator source)
                    generators.Add(source);
                else if (Activator.CreateInstance(type, nonPublic: true) is IIncrementalGenerator incremental)
                    generators.Add(incremental.AsSourceGenerator());
            }
        }

        return generators;
    }

    private static IEnumerable<CheckDiagnostic> PaginatedEfQueries(SyntaxTree tree, SemanticModel model)
    {
        foreach (var method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var methodSymbol = model.GetDeclaredSymbol(method);
            if (methodSymbol is null ||
                (!methodSymbol.Name.Contains("Search", StringComparison.OrdinalIgnoreCase) &&
                 !methodSymbol.Name.Contains("Paginat", StringComparison.OrdinalIgnoreCase)) ||
                !IsRepository(methodSymbol.ContainingType))
                continue;
            var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            var usesEf = invocations.Select(x => ResolveMethod(model, x))
                .Any(x => x?.ContainingNamespace.ToDisplayString()
                              .StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true ||
                          x?.Name is "ToListAsync" or "CountAsync") ||
                         invocations.Any(x => InvocationName(x) is "ToListAsync" or "CountAsync");
            var usesStoredProcedure = invocations.Select(x => ResolveMethod(model, x))
                .Any(x => HasStoredProcedureBoundary(x?.ContainingType));
            var paging = invocations.FirstOrDefault(invocation =>
            {
                var symbol = ResolveMethod(model, invocation);
                if (InvocationName(invocation) is not ("Skip" or "Take"))
                    return false;
                var receiver = invocation.Expression is MemberAccessExpressionSyntax access
                    ? model.GetTypeInfo(access.Expression).Type
                    : null;
                return symbol is null ||
                       symbol.ContainingType.Name == "Queryable" ||
                       symbol?.Parameters.Any(parameter =>
                           parameter.Type.OriginalDefinition.ToDisplayString().StartsWith("System.Linq.IQueryable<",
                               StringComparison.Ordinal)) == true ||
                       receiver is not null &&
                       (receiver.OriginalDefinition.ToDisplayString().StartsWith("System.Linq.IQueryable<",
                            StringComparison.Ordinal) ||
                        receiver.AllInterfaces.Any(x => x.OriginalDefinition.ToDisplayString()
                            .StartsWith("System.Linq.IQueryable<", StringComparison.Ordinal)));
            });
            if (!usesEf || paging is null)
                continue;
            var guidance = usesStoredProcedure
                ? "Calling a stored procedure elsewhere in the method does not make EF Skip/Take a stored-procedure boundary; consume the procedure result instead."
                : "Use a stored-procedure repository boundary for paginated application queries.";
            yield return Diagnostic("PE3103", "warning",
                $"{methodSymbol.ContainingType.ToDisplayString()}.{methodSymbol.Name} composes EF pagination. {guidance}",
                paging);
        }
    }

    private static IMethodSymbol? ResolveMethod(SemanticModel model, InvocationExpressionSyntax invocation)
    {
        var info = model.GetSymbolInfo(invocation);
        return info.Symbol as IMethodSymbol ?? info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
    }

    private static string? InvocationName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => null
        };

    private static IEnumerable<CheckDiagnostic> ExternalEntityMutation(SyntaxTree tree, SemanticModel model)
    {
        var writes = tree.GetRoot().DescendantNodes().Where(x =>
            x is AssignmentExpressionSyntax or PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax);
        foreach (var write in writes)
        {
            ExpressionSyntax? target = write switch
            {
                AssignmentExpressionSyntax assignment => assignment.Left,
                PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.PreIncrementExpression) ||
                                                       prefix.IsKind(SyntaxKind.PreDecrementExpression) =>
                    prefix.Operand,
                PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.PostIncrementExpression) ||
                                                         postfix.IsKind(SyntaxKind.PostDecrementExpression) =>
                    postfix.Operand,
                _ => null
            };
            if (target is null || model.GetSymbolInfo(target).Symbol is not IPropertySymbol property ||
                property.Name == "Id" || !IsEntity(property.ContainingType) ||
                HasGeneratedCode(property.ContainingType))
                continue;
            var owner = model.GetEnclosingSymbol(write.SpanStart)?.ContainingType;
            if (owner is not null && IsSameOrDerived(owner, property.ContainingType))
                continue;
            yield return Diagnostic("PE3104", "error",
                $"{owner?.ToDisplayString() ?? "<global>"} assigns entity state {property.ContainingType.ToDisplayString()}.{property.Name} directly. Call intention-revealing entity behavior instead.",
                write);
        }
    }

    private static bool IsRepository(INamedTypeSymbol? type) =>
        Hierarchy(type).Any(x =>
            x.Name.EndsWith("Repository", StringComparison.Ordinal) ||
            x.ToDisplayString().Contains(".Repositories.IRepository", StringComparison.Ordinal) ||
            x.ToDisplayString().Contains(".Repositories.RepositoryBase", StringComparison.Ordinal));

    private static bool IsEntity(INamedTypeSymbol? type) =>
        type is not null &&
        !type.Name.EndsWith("View", StringComparison.Ordinal) &&
        Hierarchy(type).Any(x =>
            x.ToDisplayString().Contains("Paradigm.Enterprise.Interfaces.IEntity", StringComparison.Ordinal) ||
            x.ToDisplayString().Contains("Paradigm.Enterprise.Domain.Entities.EntityBase", StringComparison.Ordinal));

    private static bool HasStoredProcedureBoundary(INamedTypeSymbol? type) =>
        Hierarchy(type).Any(x =>
        {
            var definition = x.OriginalDefinition;
            var providerNamespace = definition.ContainingNamespace.ToDisplayString();
            return definition.Name is "StoredProcedureBase" or "ResultStoredProcedureBase" &&
                   providerNamespace is
                       "Paradigm.Enterprise.Data.SqlServer.StoredProcedures" or
                       "Paradigm.Enterprise.Data.PostgreSql.StoredProcedures";
        });

    private static IEnumerable<INamedTypeSymbol> Hierarchy(INamedTypeSymbol? type)
    {
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        for (var current = type; current is not null && seen.Add(current); current = current.BaseType)
        {
            yield return current;
            foreach (var contract in current.AllInterfaces)
                if (seen.Add(contract))
                    yield return contract;
        }
    }

    private static bool HasGeneratedCode(INamedTypeSymbol type) =>
        type.GetAttributes().Any(x => x.AttributeClass?.Name is "GeneratedCodeAttribute" or
            "CompilerGeneratedAttribute");

    private static bool IsSameOrDerived(INamedTypeSymbol candidate, INamedTypeSymbol owner)
    {
        for (var current = candidate; current is not null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, owner))
                return true;
        return false;
    }

    private static CheckDiagnostic Diagnostic(string code, string severity, string message, SyntaxNode node)
    {
        var line = node.GetLocation().GetLineSpan();
        return new(code, severity, message,
            $"{line.Path}({line.StartLinePosition.Line + 1},{line.StartLinePosition.Character + 1})");
    }

}

internal sealed record EvaluatedProject(
    IReadOnlyList<string> SourceFiles,
    IReadOnlyList<string> ReferencePaths,
    IReadOnlyList<string> AnalyzerPaths,
    LanguageVersion LanguageVersion,
    IReadOnlyList<string> DefineConstants,
    NullableContextOptions NullableContextOptions,
    OutputKind OutputKind,
    bool AllowUnsafe)
{
    public static EvaluatedProject Load(string project, string framework)
    {
        var targetFramework = framework.Split('/')[0];
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = "dotnet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                CreateNoWindow = true,
                ArgumentList =
                {
                    "msbuild",
                    project,
                    "-nologo",
                    "-target:ResolveReferences",
                    "-getItem:Compile,ReferencePath,Analyzer",
                    "-getProperty:DefineConstants,LangVersion,Nullable,OutputType,AllowUnsafeBlocks",
                    "-property:Configuration=Release",
                    $"-property:TargetFramework={targetFramework}"
                }
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"Could not evaluate Compile items for '{project}': {error.GetAwaiter().GetResult()}");

        using var document = JsonDocument.Parse(output.GetAwaiter().GetResult());
        var root = document.RootElement;
        var properties = root.GetProperty("Properties");
        var files = root.GetProperty("Items").GetProperty("Compile").EnumerateArray()
            .Select(item => item.GetProperty("FullPath").GetString())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var references = root.GetProperty("Items").GetProperty("ReferencePath").EnumerateArray()
            .Select(item => item.GetProperty("FullPath").GetString())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var analyzers = root.GetProperty("Items").GetProperty("Analyzer").EnumerateArray()
            .Select(item => item.GetProperty("FullPath").GetString())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (files.Length == 0 || references.Length == 0)
            throw new InvalidOperationException(
                $"MSBuild evaluation for '{project}' returned {files.Length} Compile item(s) and " +
                $"{references.Length} ReferencePath item(s); semantic analysis requires both.");
        var languageValue = properties.GetProperty("LangVersion").GetString();
        var language = languageValue is not null &&
                       LanguageVersionFacts.TryParse(languageValue, out var parsedLanguage)
            ? parsedLanguage
            : LanguageVersion.Latest;
        var constants = properties.GetProperty("DefineConstants").GetString()?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        var nullable = properties.GetProperty("Nullable").GetString()?.ToLowerInvariant() switch
        {
            "enable" => NullableContextOptions.Enable,
            "annotations" => NullableContextOptions.Annotations,
            "warnings" => NullableContextOptions.Warnings,
            _ => NullableContextOptions.Disable
        };
        var outputKind = properties.GetProperty("OutputType").GetString() is "Exe" or "WinExe"
            ? OutputKind.ConsoleApplication
            : OutputKind.DynamicallyLinkedLibrary;
        var allowUnsafe = bool.TryParse(
            properties.GetProperty("AllowUnsafeBlocks").GetString(), out var unsafeValue) && unsafeValue;
        return new(files, references, analyzers, language, constants, nullable, outputKind, allowUnsafe);
    }
}

internal static class SuppressionPolicy
{
    public static bool IsSuppressed(CheckDiagnostic diagnostic, IEnumerable<CheckSuppression> suppressions) =>
        suppressions.Any(x =>
            x.Expires >= DateOnly.FromDateTime(DateTime.UtcNow) &&
            x.Code.Equals(diagnostic.Code, StringComparison.Ordinal) &&
            (string.IsNullOrWhiteSpace(x.Location) ||
             diagnostic.Location?.Contains(x.Location, StringComparison.OrdinalIgnoreCase) == true) &&
            (string.IsNullOrWhiteSpace(x.Symbol) ||
             diagnostic.Message.Contains(x.Symbol, StringComparison.Ordinal) ||
             diagnostic.Location?.Contains(x.Symbol, StringComparison.Ordinal) == true));
}

internal sealed record CheckRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("packId")] string PackId,
    [property: JsonPropertyName("packVersion")] string PackVersion,
    [property: JsonPropertyName("projects")] IReadOnlyList<CheckProject> Projects,
    [property: JsonPropertyName("suppressions")] IReadOnlyList<CheckSuppression> Suppressions);

internal sealed record CheckProject(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("framework")] string Framework);

internal sealed record CheckSuppression(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("symbol")] string? Symbol,
    [property: JsonPropertyName("location")] string? Location,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("expires")] DateOnly Expires);

internal sealed record CheckDiagnostic(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("location")] string? Location);

internal sealed record CheckResponse(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("packId")] string PackId,
    [property: JsonPropertyName("packVersion")] string PackVersion,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<CheckDiagnostic> Diagnostics);
