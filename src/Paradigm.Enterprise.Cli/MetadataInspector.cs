using System.Reflection;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;

internal sealed class MetadataInspector : IDisposable
{
    private readonly MetadataLoadContext context;
    private readonly List<Assembly> assemblies = [];
    private readonly IReadOnlyDictionary<string, (string Name, string Version)> owners;
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> documentation = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Diagnostic> diagnostics = [];

    public MetadataInspector(AssetSelection selection)
    {
        owners = selection.AssemblyOwners;
        diagnostics.AddRange(selection.Diagnostics);
        context = new MetadataLoadContext(new PathAssemblyResolver(selection.MetadataPaths));

        var candidates = selection.MetadataPaths
            .Where(path => selection.InspectionAssemblyNames.Contains(Path.GetFileNameWithoutExtension(path)))
            .Order(StringComparer.OrdinalIgnoreCase);
        foreach (var path in candidates)
        {
            try
            {
                assemblies.Add(context.LoadFromAssemblyPath(path));
                documentation[Path.GetFileNameWithoutExtension(path)] = LoadDocumentation(path);
            }
            catch (Exception exception) when (IsMetadataFailure(exception))
            {
                diagnostics.Add(MetadataDiagnostic($"Assembly '{path}' could not be loaded: {exception.Message}", path));
            }
        }
    }

    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics
        .DistinctBy(x => (x.Code, x.Message, x.Location))
        .OrderBy(x => x.Message, StringComparer.Ordinal)
        .ToArray();

    public IReadOnlyList<InspectedType> GetTypes()
    {
        var results = new List<InspectedType>();
        foreach (var assembly in assemblies.OrderBy(x => x.GetName().Name, StringComparer.OrdinalIgnoreCase))
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(x => x is not null).Cast<Type>().ToArray();
                foreach (var loaderException in exception.LoaderExceptions.Where(x => x is not null))
                    diagnostics.Add(MetadataDiagnostic(
                        $"Assembly '{assembly.GetName().Name}' was only partially loaded: {loaderException!.Message}",
                        assembly.GetName().Name));
            }
            catch (Exception exception) when (IsMetadataFailure(exception))
            {
                diagnostics.Add(MetadataDiagnostic(
                    $"Types from assembly '{assembly.GetName().Name}' could not be loaded: {exception.Message}",
                    assembly.GetName().Name));
                continue;
            }

            foreach (var type in types.OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var assemblyName = assembly.GetName().Name ?? "";
                    owners.TryGetValue(assemblyName, out var owner);
                    var structuredMembers = GetStructuredMembers(type, assemblyName);
                    results.Add(new(
                        TypeName(type),
                        TypeName(type, simple: true),
                        type.Namespace ?? "",
                        type.BaseType is null ? null : TypeName(type.BaseType),
                        type.GetInterfaces().Select(x => TypeName(x)).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
                        Attributes(type),
                        structuredMembers.Select(x => x.Signature).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
                        GetDeclaredActions(type),
                        type.IsPublic || type.IsNestedPublic,
                        type.IsAbstract,
                        assemblyName,
                        owner.Name,
                        owner.Version,
                        structuredMembers,
                        Constraints(type.GetGenericArguments())));
                }
                catch (Exception exception) when (IsMetadataFailure(exception))
                {
                    diagnostics.Add(MetadataDiagnostic(
                        $"Type '{type.FullName ?? type.Name}' could not be analyzed: {exception.Message}",
                        assembly.GetName().Name));
                }
            }
        }

        return results;
    }

    private IReadOnlyList<string> GetVisibleMembers(Type type, string assemblyName)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var members = new List<string>();
        foreach (var constructor in type.GetConstructors(flags).Where(IsVisible))
            members.Add($"{Modifiers(constructor)} {TypeName(type, true)}({Parameters(constructor.GetParameters())})");
        foreach (var property in type.GetProperties(flags))
        {
            var accessor = new[] { property.GetMethod, property.SetMethod }.Where(x => x is not null).Cast<MethodInfo>()
                .FirstOrDefault(IsVisible);
            if (accessor is null)
                continue;
            members.Add($"{Modifiers(accessor)} {TypeName(property.PropertyType)} {property.Name} {{ " +
                        $"{Accessor(property.GetMethod, "get")} {Accessor(property.SetMethod, "set")}}}");
        }
        foreach (var method in type.GetMethods(flags).Where(x => !x.IsSpecialName && IsVisible(x)))
            members.Add(MethodSignature(method));

        var docs = documentation.GetValueOrDefault(assemblyName);
        var typeDoc = docs?.GetValueOrDefault($"T:{type.FullName?.Replace('+', '.')}");
        if (!string.IsNullOrWhiteSpace(typeDoc))
            members.Insert(0, $"summary: {typeDoc}");
        return members.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private IReadOnlyList<ApiMember> GetStructuredMembers(Type type, string assemblyName)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var members = new List<ApiMember>();
        foreach (var constructor in type.GetConstructors(flags).Where(IsVisible))
        {
            var signature = $"{Modifiers(constructor)} {TypeName(type, true)}({Parameters(constructor.GetParameters())})";
            members.Add(new("constructor", TypeName(type, true), signature, null, [], Attributes(constructor), []));
        }
        foreach (var property in type.GetProperties(flags))
        {
            var visibleAccessors = new[] { property.GetMethod, property.SetMethod }
                .Where(x => x is not null).Cast<MethodInfo>().Where(IsVisible).ToArray();
            if (visibleAccessors.Length == 0)
                continue;
            var representative = visibleAccessors[0];
            var signature = $"{Modifiers(representative)} {TypeName(property.PropertyType)} {property.Name} {{ " +
                            $"{Accessor(property.GetMethod, "get")} {Accessor(property.SetMethod, "set")}}}";
            var accessors = new[] { (Method: property.GetMethod, Kind: "get"), (Method: property.SetMethod, Kind: "set") }
                .Where(x => x.Method is not null)
                .Select(x => new ApiAccessor(x.Kind, Visibility(x.Method!)))
                .ToArray();
            members.Add(new("property", property.Name, signature, TypeName(property.PropertyType), accessors,
                Attributes(property), []));
        }
        foreach (var method in type.GetMethods(flags).Where(x => !x.IsSpecialName && IsVisible(x)))
            members.Add(new("method", method.Name, MethodSignature(method), TypeName(method.ReturnType), [],
                Attributes(method), Constraints(method.GetGenericArguments())));

        var docs = documentation.GetValueOrDefault(assemblyName);
        var typeDoc = docs?.GetValueOrDefault($"T:{type.FullName?.Replace('+', '.')}");
        if (!string.IsNullOrWhiteSpace(typeDoc))
            members.Add(new("summary", "summary", $"summary: {typeDoc}", null, [], [], []));
        return members.OrderBy(x => x.Signature, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyList<InspectedAction> GetDeclaredActions(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        return type.GetMethods(flags)
            .Where(x => !x.IsSpecialName)
            .Select(x => new InspectedAction(x.Name, MethodSignature(x), Attributes(x)))
            .OrderBy(x => x.Signature, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> Attributes(MemberInfo member) =>
        CustomAttributeData.GetCustomAttributes(member)
            .Select(attribute => TypeName(attribute.AttributeType))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string MethodSignature(MethodInfo method)
    {
        var generic = method.IsGenericMethodDefinition
            ? $"<{string.Join(", ", method.GetGenericArguments().Select(x => x.Name))}>"
            : "";
        var constraints = method.IsGenericMethodDefinition ? GenericConstraints(method.GetGenericArguments()) : "";
        return $"{Modifiers(method)} {TypeName(method.ReturnType)} {method.Name}{generic}({Parameters(method.GetParameters())}){constraints}";
    }

    private static string Accessor(MethodInfo? method, string keyword)
    {
        if (method is null)
            return "";
        return IsVisible(method) ? keyword + ";" : "";
    }

    private static string GenericConstraints(IEnumerable<Type> arguments)
    {
        var values = new List<string>();
        foreach (var argument in arguments)
        {
            var constraints = argument.GetGenericParameterConstraints().Select(x => TypeName(x)).ToList();
            var attributes = argument.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
            if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
                constraints.Insert(0, "class");
            if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
                constraints.Insert(0, "struct");
            if (attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
                constraints.Add("new()");
            if (constraints.Count > 0)
                values.Add($" where {argument.Name} : {string.Join(", ", constraints)}");
        }
        return string.Concat(values);
    }

    private static IReadOnlyList<ApiGenericConstraint> Constraints(IEnumerable<Type> arguments) =>
        arguments.Where(x => x.IsGenericParameter)
            .Select(argument =>
            {
                var constraints = argument.GetGenericParameterConstraints().Select(x => TypeName(x)).ToList();
                var attributes = argument.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
                if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
                    constraints.Insert(0, "class");
                if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
                    constraints.Insert(0, "struct");
                if (attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
                    constraints.Add("new()");
                return new ApiGenericConstraint(argument.Name, constraints);
            })
            .ToArray();

    private static string Parameters(IEnumerable<ParameterInfo> parameters) =>
        string.Join(", ", parameters.Select(parameter =>
        {
            var modifier = parameter.ParameterType.IsByRef
                ? parameter.IsOut ? "out " : parameter.IsIn ? "in " : "ref "
                : "";
            return $"{modifier}{TypeName(parameter.ParameterType)} {parameter.Name}";
        }));

    private static bool IsVisible(MethodBase method) =>
        method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

    private static string Visibility(MethodBase method) =>
        method.IsPublic ? "public" :
        method.IsFamilyOrAssembly ? "protected internal" :
        method.IsFamily ? "protected" :
        method.IsAssembly ? "internal" :
        "private";

    private static string Modifiers(MethodBase method)
    {
        var values = new List<string>
        {
            method.IsPublic ? "public" : method.IsFamilyOrAssembly ? "protected internal" : "protected"
        };
        if (method.IsStatic)
            values.Add("static");
        if (method.IsAbstract)
            values.Add("abstract");
        else if (method.IsVirtual)
            values.Add(method.Attributes.HasFlag(MethodAttributes.NewSlot) ? "virtual" : "override");
        return string.Join(" ", values);
    }

    internal static string TypeName(Type type, bool simple = false)
    {
        if (type.IsGenericParameter)
            return type.Name;
        if (type.IsArray)
            return TypeName(type.GetElementType()!, simple) + "[]";
        if (type.IsByRef)
            return TypeName(type.GetElementType()!, simple);
        var name = simple ? type.Name : type.FullName ?? type.Name;
        var tick = name.IndexOf('`');
        if (tick >= 0)
            name = name[..tick];
        if (!type.IsGenericType)
            return name.Replace('+', '.');
        return $"{name.Replace('+', '.')}<{string.Join(", ", type.GetGenericArguments().Select(x => TypeName(x, simple)))}>";
    }

    private static IReadOnlyDictionary<string, string> LoadDocumentation(string assemblyPath)
    {
        var path = Path.ChangeExtension(assemblyPath, ".xml");
        if (!File.Exists(path))
            return new Dictionary<string, string>();
        try
        {
            return XDocument.Load(path).Descendants("member")
                .Where(x => x.Attribute("name") is not null)
                .GroupBy(x => x.Attribute("name")!.Value)
                .ToDictionary(
                    x => x.Key,
                    x => Normalize(x.First().Element("summary")?.Value),
                    StringComparer.Ordinal);
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static bool IsMetadataFailure(Exception exception) =>
        exception is BadImageFormatException or FileLoadException or FileNotFoundException or TypeLoadException or NotSupportedException;

    private static Diagnostic MetadataDiagnostic(string message, string? location) =>
        new("PE1002", "error", message, location);

    private static string Normalize(string? value) =>
        string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public void Dispose() => context.Dispose();
}
