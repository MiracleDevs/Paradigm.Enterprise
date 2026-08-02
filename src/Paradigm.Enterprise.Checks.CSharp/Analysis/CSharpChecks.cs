using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Paradigm.Enterprise.Checks.CSharp;

internal static class CSharpChecks
{
    #region Constants

    private const string EventArgsTypeName = "System.EventArgs";

    #endregion

    #region Public Methods

    public static IReadOnlyList<CSharpCheckDiagnostic> Analyze(CSharpCheckProject project)
    {
        var evaluated = EvaluatedProject.Load(project.Path, project.Framework);
        var parseOptions = evaluated.CreateParseOptions();
        var trees = evaluated.SourceFiles.Select(path =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(path), parseOptions, path)).ToArray();
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
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generators, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var generatedCompilation, out _);
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

        var diagnostics = new List<CSharpCheckDiagnostic>();
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            diagnostics.AddRange(SourceLayout(tree));
            diagnostics.AddRange(RegionSpacing(tree));
            diagnostics.AddRange(MemberLayout(tree, model));
            diagnostics.AddRange(PaginatedEfQueries(tree, model));
            diagnostics.AddRange(ExternalEntityMutation(tree, model));
        }

        return diagnostics
            .DistinctBy(x => (x.Code, x.Location, x.Message))
            .OrderBy(x => x.Code, StringComparer.Ordinal)
            .ThenBy(x => x.Location, StringComparer.Ordinal)
            .ThenBy(x => x.Message, StringComparer.Ordinal)
            .ToArray();
    }

    #endregion

    #region Private Methods

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
                                   AssemblyName.ReferenceMatchesDefinition(candidate.GetName(), identity))
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

    private static IEnumerable<CSharpCheckDiagnostic> SourceLayout(SyntaxTree tree)
    {
        if (IsGenerated(tree))
            yield break;
        var types = TopLevelTypes(tree.GetCompilationUnitRoot().Members).ToArray();
        if (types.Length <= 1)
            yield break;
        var names = types.Select(TypeName).Order(StringComparer.Ordinal).ToArray();
        yield return Diagnostic("PE3105", "error",
            $"The file declares {types.Length} top-level semantic types ({string.Join(", ", names)}). Keep one class, record, struct, interface, enum, or delegate per file.",
            types[1]);
    }

    private static IEnumerable<MemberDeclarationSyntax> TopLevelTypes(
        IEnumerable<MemberDeclarationSyntax> members)
    {
        foreach (var member in members)
            switch (member)
            {
                case BaseNamespaceDeclarationSyntax @namespace:
                    foreach (var nested in TopLevelTypes(@namespace.Members))
                        yield return nested;
                    break;
                case BaseTypeDeclarationSyntax or DelegateDeclarationSyntax:
                    yield return member;
                    break;
            }
    }

    private static string TypeName(MemberDeclarationSyntax member) => member switch
    {
        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
        DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
        _ => "<unknown>"
    };

    private static IEnumerable<CSharpCheckDiagnostic> MemberLayout(
        SyntaxTree tree,
        SemanticModel model)
    {
        if (IsGenerated(tree))
            yield break;
        var root = tree.GetRoot();
        var regions = Regions(root);
        var declarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Where(type => type is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax);
        foreach (var declaration in declarations)
        {
            var previousRank = -1;
            foreach (var member in declaration.Members)
            {
                var category = MemberCategory(member, model);
                if (category is null)
                    continue;
                var (rank, expectedRegion) = category.Value;
                if (rank < previousRank)
                    yield return Diagnostic("PE3106", "error",
                        $"{TypeName(declaration)}.{MemberName(member)} is out of order. Use the canonical member-region order and place it in '#region {expectedRegion}'.",
                        member);
                previousRank = Math.Max(previousRank, rank);
                var actualRegion = regions
                    .Where(region => region.Start <= member.SpanStart && member.SpanStart < region.End)
                    .OrderByDescending(region => region.Start)
                    .Select(region => region.Name)
                    .FirstOrDefault();
                if (!string.Equals(actualRegion, expectedRegion, StringComparison.Ordinal))
                    yield return Diagnostic("PE3106", "error",
                        $"{TypeName(declaration)}.{MemberName(member)} must be inside '#region {expectedRegion}' (actual: {(actualRegion is null ? "no region" : $"#region {actualRegion}")}).",
                        member);
            }
        }
    }

    private static IEnumerable<CSharpCheckDiagnostic> RegionSpacing(SyntaxTree tree)
    {
        if (IsGenerated(tree))
            yield break;
        var root = tree.GetRoot();
        var text = tree.GetText();
        var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>().ToArray();
        var directives = root.DescendantTrivia(descendIntoTrivia: true)
            .Select(trivia => trivia.GetStructure())
            .OfType<DirectiveTriviaSyntax>()
            .Where(directive => directive is RegionDirectiveTriviaSyntax or EndRegionDirectiveTriviaSyntax)
            .OrderBy(directive => directive.SpanStart);
        var stack = new Stack<RegionDirectiveTriviaSyntax>();
        foreach (var directive in directives)
        {
            if (directive is RegionDirectiveTriviaSyntax region)
            {
                stack.Push(region);
                continue;
            }

            if (directive is not EndRegionDirectiveTriviaSyntax endRegion ||
                !stack.TryPop(out var openingRegion) ||
                !IsMemberRegion(openingRegion.ToString()) ||
                !types.Any(type => type.FullSpan.Contains(openingRegion.SpanStart)))
                continue;

            var openingLine = text.Lines.GetLineFromPosition(openingRegion.SpanStart).LineNumber;
            if (!HasExactlyOneEmptyLineAfter(text, openingLine))
                yield return Diagnostic("PE3106", "error",
                    $"Leave exactly one empty line after '{openingRegion.ToString().Trim()}'.",
                    openingRegion);

            var closingLine = text.Lines.GetLineFromPosition(endRegion.SpanStart).LineNumber;
            if (!HasExactlyOneEmptyLineBefore(text, closingLine))
                yield return Diagnostic("PE3106", "error",
                    "Leave exactly one empty line before '#endregion'.",
                    endRegion);

            var nextContentLine = closingLine + 1;
            while (IsEmptyLine(text, nextContentLine))
                nextContentLine++;
            if (nextContentLine < text.Lines.Count &&
                IsMemberRegion(text.Lines[nextContentLine].ToString()) &&
                nextContentLine != closingLine + 2)
                yield return Diagnostic("PE3106", "error",
                    "Leave exactly one empty line between adjacent member regions.",
                    endRegion);
        }
    }

    private static bool HasExactlyOneEmptyLineAfter(SourceText text, int lineNumber) =>
        IsEmptyLine(text, lineNumber + 1) && !IsEmptyLine(text, lineNumber + 2);

    private static bool HasExactlyOneEmptyLineBefore(SourceText text, int lineNumber) =>
        IsEmptyLine(text, lineNumber - 1) && !IsEmptyLine(text, lineNumber - 2);

    private static bool IsEmptyLine(SourceText text, int lineNumber) =>
        lineNumber >= 0 && lineNumber < text.Lines.Count &&
        string.IsNullOrWhiteSpace(text.Lines[lineNumber].ToString());

    private static bool IsMemberRegion(string value) => value.Trim() is
        "#region Nested Types" or
        "#region Constants" or
        "#region Fields" or
        "#region Properties" or
        "#region Constructors" or
        "#region Static Constructors" or
        "#region Public Methods" or
        "#region Overrides" or
        "#region Protected Methods" or
        "#region Private Methods" or
        "#region Event Handlers";

    private static (int Rank, string Region)? MemberCategory(
        MemberDeclarationSyntax member,
        SemanticModel model) => member switch
        {
            BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => (0, "Nested Types"),
            FieldDeclarationSyntax field when field.Modifiers.Any(SyntaxKind.ConstKeyword) => (1, "Constants"),
            FieldDeclarationSyntax or EventFieldDeclarationSyntax => (2, "Fields"),
            PropertyDeclarationSyntax or IndexerDeclarationSyntax or EventDeclarationSyntax => (3, "Properties"),
            ConstructorDeclarationSyntax constructor when !constructor.Modifiers.Any(SyntaxKind.StaticKeyword) =>
                (4, "Constructors"),
            ConstructorDeclarationSyntax => (5, "Static Constructors"),
            MethodDeclarationSyntax method when IsEventHandler(method, model) => (10, "Event Handlers"),
            BaseMethodDeclarationSyntax method when method.Modifiers.Any(SyntaxKind.OverrideKeyword) =>
                (7, "Overrides"),
            BaseMethodDeclarationSyntax method when method.Modifiers.Any(SyntaxKind.PublicKeyword) =>
                (6, "Public Methods"),
            BaseMethodDeclarationSyntax method when method.Modifiers.Any(SyntaxKind.ProtectedKeyword) =>
                (8, "Protected Methods"),
            BaseMethodDeclarationSyntax => (9, "Private Methods"),
            _ => null
        };

    private static bool IsEventHandler(MethodDeclarationSyntax method, SemanticModel model)
    {
        if (method.ParameterList.Parameters.Count != 2 ||
            model.GetDeclaredSymbol(method) is not IMethodSymbol symbol || !symbol.ReturnsVoid)
            return false;
        for (var current = symbol.Parameters[1].Type as INamedTypeSymbol;
             current is not null;
             current = current.BaseType)
            if (current.ToDisplayString().Equals(EventArgsTypeName, StringComparison.Ordinal))
                return true;
        return false;
    }

    private static string MemberName(MemberDeclarationSyntax member) => member switch
    {
        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
        DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
        FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText ?? "<field>",
        EventFieldDeclarationSyntax @event => @event.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText ?? "<event>",
        PropertyDeclarationSyntax property => property.Identifier.ValueText,
        EventDeclarationSyntax @event => @event.Identifier.ValueText,
        IndexerDeclarationSyntax => "this[]",
        ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
        MethodDeclarationSyntax method => method.Identifier.ValueText,
        OperatorDeclarationSyntax @operator => $"operator {@operator.OperatorToken.ValueText}",
        ConversionOperatorDeclarationSyntax conversion => $"operator {conversion.Type}",
        DestructorDeclarationSyntax destructor => $"~{destructor.Identifier.ValueText}",
        _ => "<member>"
    };

    private static IReadOnlyList<RegionRange> Regions(SyntaxNode root)
    {
        var ranges = new List<RegionRange>();
        var stack = new Stack<(string Name, int Start)>();
        foreach (var directive in root.DescendantTrivia(descendIntoTrivia: true)
                     .Select(trivia => trivia.GetStructure())
                     .OfType<DirectiveTriviaSyntax>()
                     .OrderBy(directive => directive.SpanStart))
            switch (directive)
            {
                case RegionDirectiveTriviaSyntax region:
                    stack.Push((region.ToString()["#region".Length..].Trim(), region.FullSpan.End));
                    break;
                case EndRegionDirectiveTriviaSyntax when stack.Count > 0:
                    var start = stack.Pop();
                    ranges.Add(new(start.Name, start.Start, directive.FullSpan.Start));
                    break;
            }
        return ranges;
    }

    private static bool IsGenerated(SyntaxTree tree)
    {
        var path = tree.FilePath;
        if (path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
            return true;
        var text = tree.GetText();
        return text.ToString(new(0, Math.Min(text.Length, 2048)))
            .Contains("<auto-generated", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<CSharpCheckDiagnostic> PaginatedEfQueries(
        SyntaxTree tree,
        SemanticModel model)
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
            var usesEf = invocations.Select(invocation => ResolveMethod(model, invocation))
                .Any(symbol => symbol?.ContainingNamespace.ToDisplayString()
                                   .StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true ||
                               symbol?.Name is "ToListAsync" or "CountAsync") ||
                         invocations.Any(invocation => InvocationName(invocation) is "ToListAsync" or "CountAsync");
            var usesStoredProcedure = invocations.Select(invocation => ResolveMethod(model, invocation))
                .Any(symbol => HasStoredProcedureBoundary(symbol?.ContainingType));
            var paging = invocations.FirstOrDefault(invocation =>
            {
                var symbol = ResolveMethod(model, invocation);
                if (InvocationName(invocation) is not ("Skip" or "Take"))
                    return false;
                var receiver = invocation.Expression is MemberAccessExpressionSyntax access
                    ? model.GetTypeInfo(access.Expression).Type
                    : null;
                return symbol is null || symbol.ContainingType.Name == "Queryable" ||
                       symbol.Parameters.Any(parameter =>
                           parameter.Type.OriginalDefinition.ToDisplayString().StartsWith(
                               "System.Linq.IQueryable<", StringComparison.Ordinal)) ||
                       receiver is not null &&
                       (receiver.OriginalDefinition.ToDisplayString().StartsWith(
                            "System.Linq.IQueryable<", StringComparison.Ordinal) ||
                        receiver.AllInterfaces.Any(contract => contract.OriginalDefinition.ToDisplayString()
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

    private static IMethodSymbol? ResolveMethod(
        SemanticModel model,
        InvocationExpressionSyntax invocation)
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

    private static IEnumerable<CSharpCheckDiagnostic> ExternalEntityMutation(
        SyntaxTree tree,
        SemanticModel model)
    {
        var writes = tree.GetRoot().DescendantNodes().Where(node =>
            node is AssignmentExpressionSyntax or PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax);
        foreach (var write in writes)
        {
            ExpressionSyntax? target = write switch
            {
                AssignmentExpressionSyntax assignment => assignment.Left,
                PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.PreIncrementExpression) ||
                                                       prefix.IsKind(SyntaxKind.PreDecrementExpression) => prefix.Operand,
                PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.PostIncrementExpression) ||
                                                         postfix.IsKind(SyntaxKind.PostDecrementExpression) =>
                    postfix.Operand,
                _ => null
            };
            if (target is null || model.GetSymbolInfo(target).Symbol is not IPropertySymbol property ||
                property.Name == "Id" || !IsEntity(property.ContainingType) || HasGeneratedCode(property.ContainingType))
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
        Hierarchy(type).Any(candidate =>
            candidate.Name.EndsWith("Repository", StringComparison.Ordinal) ||
            candidate.ToDisplayString().Contains(".Repositories.IRepository", StringComparison.Ordinal) ||
            candidate.ToDisplayString().Contains(".Repositories.RepositoryBase", StringComparison.Ordinal));

    private static bool IsEntity(INamedTypeSymbol? type) =>
        type is not null && !type.Name.EndsWith("View", StringComparison.Ordinal) &&
        Hierarchy(type).Any(candidate =>
            candidate.ToDisplayString().Contains("Paradigm.Enterprise.Interfaces.IEntity", StringComparison.Ordinal) ||
            candidate.ToDisplayString().Contains("Paradigm.Enterprise.Domain.Entities.EntityBase", StringComparison.Ordinal));

    private static bool HasStoredProcedureBoundary(INamedTypeSymbol? type) =>
        Hierarchy(type).Any(candidate =>
        {
            var definition = candidate.OriginalDefinition;
            var providerNamespace = definition.ContainingNamespace.ToDisplayString();
            return definition.Name is "StoredProcedureBase" or "ResultStoredProcedureBase" &&
                   providerNamespace is "Paradigm.Enterprise.Data.SqlServer.StoredProcedures" or
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
        type.GetAttributes().Any(attribute => attribute.AttributeClass?.Name is
            "GeneratedCodeAttribute" or "CompilerGeneratedAttribute");

    private static bool IsSameOrDerived(INamedTypeSymbol candidate, INamedTypeSymbol owner)
    {
        for (var current = candidate; current is not null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, owner))
                return true;
        return false;
    }

    private static CSharpCheckDiagnostic Diagnostic(
        string code,
        string severity,
        string message,
        SyntaxNode node)
    {
        var line = node.GetLocation().GetLineSpan();
        return new(code, severity, message,
            $"{line.Path}({line.StartLinePosition.Line + 1},{line.StartLinePosition.Character + 1})");
    }

    #endregion
}
