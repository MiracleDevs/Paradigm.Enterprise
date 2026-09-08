using System.Data;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Paradigm.Enterprise.Checks.CSharp;

internal static class RepositorySqlPolicy
{
    #region Constants

    private const string RepositoryTypeName = "Paradigm.Enterprise.Domain.Repositories.IRepository";
    private const string DbConnectionTypeName = "System.Data.Common.DbConnection";
    private const string DbCommandTypeName = "System.Data.Common.DbCommand";
    private const string ConnectionContractTypeName = "System.Data.IDbConnection";
    private const string CommandContractTypeName = "System.Data.IDbCommand";
    private const string SqlServerStoredProcedureNamespace = "Paradigm.Enterprise.Data.SqlServer.StoredProcedures";
    private const string PostgreSqlStoredProcedureNamespace = "Paradigm.Enterprise.Data.PostgreSql.StoredProcedures";

    #endregion

    #region Public Methods

    public static IReadOnlyList<CSharpCheckDiagnostic> Analyze(
        CSharpCompilation compilation,
        IEnumerable<SyntaxTree> trees)
    {
        var repositoryContract = compilation.GetTypeByMetadataName(RepositoryTypeName);
        if (repositoryContract is null)
            return [];

        var diagnostics = new List<CSharpCheckDiagnostic>();
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(declaration) is not { } repository ||
                    !IsAssignableTo(repository, repositoryContract))
                    continue;

                diagnostics.AddRange(AnalyzeRepository(declaration, repository, model, compilation));
            }
        }

        return diagnostics;
    }

    #endregion

    #region Private Methods

    private static IEnumerable<CSharpCheckDiagnostic> AnalyzeRepository(
        ClassDeclarationSyntax declaration,
        INamedTypeSymbol repository,
        SemanticModel model,
        Compilation compilation)
    {
        foreach (var invocation in OwnedNodes<InvocationExpressionSyntax>(declaration, repository, model))
        {
            var method = ResolveMethod(model, invocation);
            var construct = RawInvocation(method, invocation, model, compilation);
            if (construct is not null)
                yield return Diagnostic(repository, model, invocation, construct);
        }

        foreach (var assignment in OwnedNodes<AssignmentExpressionSyntax>(declaration, repository, model))
        {
            var target = model.GetSymbolInfo(assignment.Left).Symbol;
            if (target is IPropertySymbol property &&
                property.Name == nameof(IDbCommand.CommandText) &&
                IsCommand(property.ContainingType, compilation))
            {
                yield return Diagnostic(repository, model, assignment,
                    property.ContainingType.ToDisplayString() + "." + property.Name);
                continue;
            }

            if (target is IFieldSymbol or IPropertySymbol &&
                SymbolEqualityComparer.Default.Equals(target.ContainingType, repository) &&
                IsSqlExpression(assignment.Right, model))
                yield return Diagnostic(repository, model, assignment, "SQL-bearing assignment");
        }

        foreach (var creation in OwnedNodes<ObjectCreationExpressionSyntax>(declaration, repository, model))
        {
            var type = model.GetTypeInfo(creation).Type;
            if (IsCommand(type, compilation))
                yield return Diagnostic(repository, model, creation,
                    type!.ToDisplayString() + ".ctor");
        }

        foreach (var variable in OwnedNodes<VariableDeclaratorSyntax>(declaration, repository, model))
        {
            if (variable.Initializer is null ||
                model.GetDeclaredSymbol(variable) is not ILocalSymbol and not IFieldSymbol ||
                !IsSqlExpression(variable.Initializer.Value, model))
                continue;
            yield return Diagnostic(repository, model, variable, "SQL-bearing declaration");
        }

        foreach (var property in OwnedNodes<PropertyDeclarationSyntax>(declaration, repository, model))
        {
            var expressions = property.ExpressionBody is not null
                ? new[] { property.ExpressionBody.Expression }
                : property.Initializer is not null
                    ? new[] { property.Initializer.Value }
                    : property.AccessorList?.Accessors.SelectMany(ReturnedExpressions).ToArray() ?? [];
            if (expressions.Any(expression => IsSqlExpression(expression, model)))
                yield return Diagnostic(repository, model, property, "SQL-bearing declaration");
        }

        foreach (var method in OwnedNodes<MethodDeclarationSyntax>(declaration, repository, model))
            if (ReturnedExpressions(method).Any(expression => IsSqlExpression(expression, model)))
                yield return Diagnostic(repository, model, method, "SQL-bearing declaration");

        foreach (var localFunction in OwnedNodes<LocalFunctionStatementSyntax>(declaration, repository, model))
            if (ReturnedExpressions(localFunction).Any(expression => IsSqlExpression(expression, model)))
                yield return Diagnostic(repository, model, localFunction, "SQL-bearing declaration");
    }

    private static IEnumerable<TNode> OwnedNodes<TNode>(
        ClassDeclarationSyntax declaration,
        INamedTypeSymbol repository,
        SemanticModel model)
        where TNode : SyntaxNode => declaration.DescendantNodes()
        .OfType<TNode>()
        .Where(node =>
        {
            return SymbolEqualityComparer.Default.Equals(
                DeclaredSymbol(node, model)?.ContainingType ?? model.GetEnclosingSymbol(node.SpanStart)?.ContainingType,
                repository);
        });

    private static ISymbol? DeclaredSymbol(SyntaxNode node, SemanticModel model) => node switch
    {
        MethodDeclarationSyntax method => model.GetDeclaredSymbol(method),
        PropertyDeclarationSyntax property => model.GetDeclaredSymbol(property),
        VariableDeclaratorSyntax variable => model.GetDeclaredSymbol(variable),
        LocalFunctionStatementSyntax localFunction => model.GetDeclaredSymbol(localFunction),
        _ => null
    };

    private static string? RawInvocation(
        IMethodSymbol? method,
        InvocationExpressionSyntax invocation,
        SemanticModel model,
        Compilation compilation)
    {
        if (method is null || HasStoredProcedureBoundary(ReceiverType(invocation, model)))
            return null;

        var original = method.ReducedFrom ?? method;
        var name = original.Name;
        var containingNamespace = original.ContainingNamespace.ToDisplayString();
        var resolvedApi = original.ContainingType.ToDisplayString() + "." + name;

        if (containingNamespace.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) &&
            (name.StartsWith("FromSql", StringComparison.Ordinal) ||
             name.StartsWith("ExecuteSql", StringComparison.Ordinal) ||
             name.StartsWith("SqlQuery", StringComparison.Ordinal)))
            return resolvedApi;

        var receiverType = ReceiverType(invocation, model);
        if (name == nameof(IDbConnection.CreateCommand) && IsConnection(receiverType, compilation))
            return resolvedApi;

        if (name.StartsWith("ExecuteReader", StringComparison.Ordinal) ||
            name.StartsWith("ExecuteScalar", StringComparison.Ordinal) ||
            name.StartsWith("ExecuteNonQuery", StringComparison.Ordinal))
            if (IsCommand(receiverType, compilation))
                return resolvedApi;

        if (!IsSqlExecutionName(name) || !IsConnection(receiverType, compilation))
            return null;

        var isDapper = containingNamespace.Equals("Dapper", StringComparison.Ordinal) ||
                       containingNamespace.StartsWith("Dapper.", StringComparison.Ordinal) ||
                       original.ContainingAssembly.Name.Equals("Dapper", StringComparison.OrdinalIgnoreCase);
        var hasSqlParameter = original.Parameters.Any(parameter => IsSqlParameterType(parameter.Type));
        return isDapper || original.IsExtensionMethod && hasSqlParameter ? resolvedApi : null;
    }

    private static IMethodSymbol? ResolveMethod(SemanticModel model, InvocationExpressionSyntax invocation) =>
        model.GetSymbolInfo(invocation).Symbol as IMethodSymbol ??
        model.GetSymbolInfo(invocation).CandidateSymbols.OfType<IMethodSymbol>().SingleOrDefault();

    private static ITypeSymbol? ReceiverType(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model.GetOperation(invocation) is not IInvocationOperation operation)
            return null;
        if (operation.Instance?.Type is { } instanceType)
            return instanceType;
        if (operation.TargetMethod is { ReducedFrom: null, IsExtensionMethod: true })
            return operation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 0)?.Value.Type;
        return null;
    }

    private static bool IsSqlParameterType(ITypeSymbol type) =>
        type.SpecialType == SpecialType.System_String ||
        type.ToDisplayString() == "System.FormattableString";

    private static bool IsSqlExecutionName(string name) =>
        name.StartsWith("Query", StringComparison.Ordinal) ||
        name.StartsWith("Execute", StringComparison.Ordinal) ||
        name.StartsWith("ExecuteReader", StringComparison.Ordinal) ||
        name.StartsWith("ExecuteScalar", StringComparison.Ordinal);

    private static bool IsConnection(ITypeSymbol? type, Compilation compilation) =>
        IsAssignableTo(type, compilation.GetTypeByMetadataName(DbConnectionTypeName)) ||
        IsAssignableTo(type, compilation.GetTypeByMetadataName(ConnectionContractTypeName));

    private static bool IsCommand(ITypeSymbol? type, Compilation compilation) =>
        IsAssignableTo(type, compilation.GetTypeByMetadataName(DbCommandTypeName)) ||
        IsAssignableTo(type, compilation.GetTypeByMetadataName(CommandContractTypeName));

    private static bool IsAssignableTo(ITypeSymbol? candidate, ITypeSymbol? contract)
    {
        if (candidate is null || contract is null)
            return false;
        if (SymbolEqualityComparer.Default.Equals(candidate, contract))
            return true;
        if (candidate is INamedTypeSymbol named)
        {
            if (named.AllInterfaces.Any(item => SymbolEqualityComparer.Default.Equals(item, contract)))
                return true;
            for (var current = named.BaseType; current is not null; current = current.BaseType)
                if (SymbolEqualityComparer.Default.Equals(current, contract))
                    return true;
        }
        return false;
    }

    private static bool HasStoredProcedureBoundary(ITypeSymbol? type)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            var definition = current.OriginalDefinition;
            var providerNamespace = definition.ContainingNamespace.ToDisplayString();
            if (definition.Name is "StoredProcedureBase" or "ResultStoredProcedureBase" &&
                providerNamespace is SqlServerStoredProcedureNamespace or PostgreSqlStoredProcedureNamespace)
                return true;
        }
        return false;
    }

    private static IEnumerable<ExpressionSyntax> ReturnedExpressions(MethodDeclarationSyntax method) =>
        method.ExpressionBody is not null
            ? [method.ExpressionBody.Expression]
            : method.Body?.DescendantNodes().OfType<ReturnStatementSyntax>()
                .Where(statement => statement.Expression is not null)
                .Select(statement => statement.Expression!).ToArray() ?? [];

    private static IEnumerable<ExpressionSyntax> ReturnedExpressions(LocalFunctionStatementSyntax method) =>
        method.ExpressionBody is not null
            ? [method.ExpressionBody.Expression]
            : method.Body?.DescendantNodes().OfType<ReturnStatementSyntax>()
                .Where(statement => statement.Expression is not null)
                .Select(statement => statement.Expression!).ToArray() ?? [];

    private static IEnumerable<ExpressionSyntax> ReturnedExpressions(AccessorDeclarationSyntax accessor) =>
        accessor.ExpressionBody is not null
            ? [accessor.ExpressionBody.Expression]
            : accessor.Body?.DescendantNodes().OfType<ReturnStatementSyntax>()
                .Where(statement => statement.Expression is not null)
                .Select(statement => statement.Expression!).ToArray() ?? [];

    private static bool IsSqlExpression(ExpressionSyntax expression, SemanticModel model) =>
        IsSqlExpression(expression, model, new HashSet<ISymbol>(SymbolEqualityComparer.Default));

    private static bool IsSqlExpression(
        ExpressionSyntax expression,
        SemanticModel model,
        ISet<ISymbol> visited)
    {
        var constant = model.GetConstantValue(expression);
        if (constant.HasValue && constant.Value is string value)
            return IsSqlStatement(value);

        if (expression is InterpolatedStringExpressionSyntax interpolated)
        {
            var text = string.Concat(interpolated.Contents.Select(content => content switch
            {
                InterpolatedStringTextSyntax literal => literal.TextToken.ValueText,
                InterpolationSyntax => " value ",
                _ => string.Empty
            }));
            return IsSqlStatement(text);
        }

        var symbol = model.GetSymbolInfo(expression).Symbol;
        if (symbol is not null && visited.Add(symbol))
        {
            foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
            {
                var declaration = syntaxReference.GetSyntax();
                var declarationModel = model.Compilation.GetSemanticModel(declaration.SyntaxTree, true);
                var values = declaration switch
                {
                    VariableDeclaratorSyntax variable when variable.Initializer is not null =>
                        new[] { variable.Initializer.Value },
                    PropertyDeclarationSyntax property when property.ExpressionBody is not null =>
                        new[] { property.ExpressionBody.Expression },
                    PropertyDeclarationSyntax property when property.Initializer is not null =>
                        new[] { property.Initializer.Value },
                    MethodDeclarationSyntax method => ReturnedExpressions(method).ToArray(),
                    LocalFunctionStatementSyntax localFunction => ReturnedExpressions(localFunction).ToArray(),
                    _ => []
                };
                if (values.Any(value => IsSqlExpression(value, declarationModel, visited)))
                    return true;
            }
        }
        return false;
    }

    private static bool IsSqlStatement(string value)
    {
        var tokens = value.TrimStart()
            .Split([' ', '\t', '\r', '\n', '(', ')', ',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim('[', ']', '"', '`').ToUpperInvariant())
            .ToArray();
        if (tokens.Length < 2)
            return false;
        return tokens[0] switch
        {
            "SELECT" => tokens.Contains("FROM") || tokens.Contains("JOIN") || tokens.Contains("INTO") ||
                        tokens[1] == "*" || tokens[1].StartsWith("@@", StringComparison.Ordinal) ||
                        ulong.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "INSERT" => tokens.Contains("INTO") && (tokens.Contains("VALUES") || tokens.Contains("SELECT")),
            "UPDATE" => tokens.Contains("SET"),
            "DELETE" => tokens.Contains("FROM"),
            "MERGE" => tokens.Contains("INTO") && tokens.Contains("USING"),
            "WITH" => tokens.Contains("SELECT") && (tokens.Contains("FROM") || tokens.Contains("JOIN")),
            "EXEC" or "EXECUTE" => tokens.Length >= 2 &&
                                   (tokens[1].Contains('.', StringComparison.Ordinal) ||
                                    tokens.Skip(2).Any(token =>
                                        token.StartsWith('@') || token.Contains('=', StringComparison.Ordinal))),
            "CREATE" or "ALTER" or "DROP" => tokens[1] is "TABLE" or "VIEW" or "PROCEDURE" or "PROC" or "FUNCTION" or "INDEX" or "SCHEMA",
            _ => false
        };
    }

    private static CSharpCheckDiagnostic Diagnostic(
        INamedTypeSymbol repository,
        SemanticModel model,
        SyntaxNode node,
        string construct)
    {
        var member = DeclaredSymbol(node, model) ?? model.GetEnclosingSymbol(node.SpanStart);
        if (member is ILocalSymbol)
            member = member.ContainingSymbol;
        while (member is not null && member.ContainingType is not null &&
               !SymbolEqualityComparer.Default.Equals(member.ContainingType, repository))
            member = member.ContainingSymbol;
        var memberName = member switch
        {
            null or INamedTypeSymbol => repository.ToDisplayString(),
            IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } =>
                repository.ToDisplayString() + "." + repository.Name,
            _ => repository.ToDisplayString() + "." + member.Name
        };
        var line = node.GetLocation().GetLineSpan();
        return new("PE3107", "error",
            $"{memberName} uses handwritten SQL or raw database command '{construct}'. Use EF/LINQ for simple bounded work or a typed stored-procedure wrapper whose SQL lives in the database project.",
            $"{line.Path}({line.StartLinePosition.Line + 1},{line.StartLinePosition.Character + 1})");
    }

    #endregion
}
