namespace Paradigm.Enterprise.Cli;
internal sealed class EntitySetterRule : IValidationRule
{
#region Properties
    public string Code => "PE3101";

#endregion
#region Public Methods
    public IEnumerable<Diagnostic> Evaluate(ValidationContext context)
    {
        foreach (var type in context.ApplicationTypes)
        {
            if (Analysis.Classify(type, context.AllTypes) != "entity" || HasGeneratedMarker(type.Attributes))
                continue;
            foreach (var property in type.StructuredMembers?.Where(x => x.Kind == "property") ?? [])
            {
                if (property.Name == "Id" || HasGeneratedMarker(property.Attributes) || !property.Accessors.Any(x => x.Kind == "set" && x.Visibility == "public"))
                    continue;
                yield return new(Code, "warning", $"{type.FullName}.{property.Name} has a public setter. Handwritten entities should expose intention-revealing behavior and non-public setters.", $"{type.AssemblyName}:{type.FullName}.{property.Name}");
            }
        }
    }

#endregion
#region Private Methods
    private static bool HasGeneratedMarker(IEnumerable<string> attributes) => attributes.Any(attribute => attribute.EndsWith(".GeneratedCodeAttribute", StringComparison.Ordinal) || attribute.EndsWith(".CompilerGeneratedAttribute", StringComparison.Ordinal) || attribute is "System.CodeDom.Compiler.GeneratedCodeAttribute" or "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
#endregion
}
