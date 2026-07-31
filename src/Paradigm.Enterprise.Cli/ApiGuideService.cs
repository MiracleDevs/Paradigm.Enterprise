namespace Paradigm.Enterprise.Cli;
internal sealed class ApiGuideService
{
#region Nested Types
    private sealed record Descriptor(string Pattern, IReadOnlyList<string> Discovery, IReadOnlyList<string> Cautions, IReadOnlyList<string> Verification, Func<InspectedType, bool>? Probe = null);
#endregion
#region Public Methods
    public GuideInfo? Create(InspectedType type)
    {
        var key = Ungeneric(type.Name);
        var descriptor = DescriptorFor(key);
        if (descriptor is null || descriptor.Probe?.Invoke(type) == false)
            return null;
        var members = type.StructuredMembers ?? [];
        var required = members.Where(x => x.Kind == "method" && (type.Name.StartsWith("I", StringComparison.Ordinal) || x.Signature.Contains(" abstract ", StringComparison.Ordinal))).Select(x => x.Signature).Take(20).ToArray();
        var hooks = members.Where(x => x.Kind == "method" && x.Signature.StartsWith("protected ", StringComparison.Ordinal) && x.Signature.Contains("virtual", StringComparison.Ordinal)).Select(x => x.Signature).Take(20).ToArray();
        var generic = (type.GenericConstraints ?? []).Select(x => x.Constraints.Count == 0 ? x.Parameter : $"{x.Parameter}: {string.Join(", ", x.Constraints)}").ToArray();
        return new(type.FullName, descriptor.Pattern, generic, required, hooks, descriptor.Discovery, descriptor.Cautions, descriptor.Verification, new(type.FullName, type.Namespace, type.BaseType, type.Interfaces, type.Attributes, type.GenericConstraints ?? [], members));
    }

#endregion
#region Private Methods
    private static Descriptor? DescriptorFor(string symbol) => symbol switch
    {
        "IEntity" or "EntityBase" => new("Implement the capability's exact entity interface and derive the installed EntityBase<TId> shape; keep handwritten state setters non-public and expose behavior methods.", ["Register concrete entities only when provider construction resolves them; keep shared entity interfaces getter-only."], ["Use one value-type identifier across the complete capability.", "Keep EntityBase<TId>.Id unchanged; identity tightening is a separate breaking decision.", "Entity-owned mapping should call behavior for transition-sensitive state."], ["paradigm validate --project <solution>", "dotnet test <domain-tests>"]),
        "IReadRepository" or "IEditRepository" or "IRepository" or "RepositoryBase" or "ReadRepositoryBase" or "EditRepositoryBase" => new("Define an exact-name capability contract (for example IOrderRepository) and implement it with the narrowest installed repository base.", ["The public concrete OrderRepository must implement exactly one IOrderRepository capability interface.", "Register the capability interface and concrete repository as scoped."], ["Never expose IQueryable, DbContext, or provider-specific connections.", "Prefer a stored-procedure boundary for pagination, complex filtering, multi-join/reporting, and multi-step database work.", "Repository writes stage changes; the Provider/Unit of Work owns commit timing."], ["paradigm validate --project <solution>", "paradigm checks run --project <solution>"]),
        "IProvider" or "IReadProvider" or "ProviderBase" or "ReadProviderBase" => new("Define an exact-name capability interface and derive the installed provider base to orchestrate repositories, validation, authorization, and lifecycle hooks.", ["The public concrete OrderProvider must implement exactly one IOrderProvider interface.", "Register providers as scoped unless the host's documented lifetime model requires otherwise."], ["Do not put HTTP mechanics or concrete database queries in Providers.", "Resolve every transaction participant before opening an explicit Unit of Work transaction.", "Do not claim external side effects are atomic with database commits; use an outbox when required."], ["paradigm validate --project <solution>", "dotnet test <provider-tests>"]),
        "IEditProvider" => new("Normal pattern: define the exact-name capability interface (IOrderProvider) inheriting IEditProvider<TView,TId>, then implement it with EditProviderBase<...>. Direct IEditProvider implementation is an advanced exception requiring complete lifecycle, mapping, validation, commit, and reload behavior.", ["The concrete provider name and capability interface must match exactly.", "Register the exact-name interface to the EditProviderBase-derived implementation as scoped."], ["Map into the loaded entity, validate it, stage through the edit repository, and commit through IUnitOfWork.", "Keep authorization and caller identity decisions in the Provider; keep HTTP details outside it.", "After-hooks run around the installed base lifecycle; verify the reflected installed order before adding irreversible effects."], ["paradigm api show EditProviderBase --project <solution>", "paradigm validate --project <solution>", "dotnet test <provider-tests>"], type => type.Interfaces.Any(x => x.Contains("IReadProvider", StringComparison.Ordinal)) || (type.StructuredMembers ?? []).Any(x => x.Name == "SaveAsync")),
        "EditProviderBase" => new("Derive a capability provider from EditProviderBase and expose it through an exact-name interface inheriting IEditProvider.", ["Register the exact-name interface and concrete provider as scoped.", "Ensure entity, view, repositories, and identifier generic arguments describe one capability."], ["Use entity-owned mapping behavior for transition-sensitive state.", "Treat hooks according to their reflected order; use an outbox for effects requiring durability with the database."], ["paradigm api guide IEditProvider --project <solution>", "paradigm validate --project <solution>", "dotnet test <provider-tests>"]),
        "ApiControllerBase" or "ReadApiControllerBase" or "EditApiControllerBase" => new("Derive the narrowest installed controller base and expose only the required actions through a capability Provider.", ["Register Providers and explicit module assembly roots; endpoint exposure and authorization are independent.", "Apply an independent authorization policy/filter to every reachable action or the controller hierarchy."], ["Framework controller bases may carry anonymous metadata; never assume exposure implies authorization.", "Keep mapping, transactions, and business decisions out of controllers.", "Update source-generated JSON contexts for request and response types."], ["paradigm validate --project <solution>", "dotnet test <web-api-integration-tests>"]),
        "IUnitOfWork" or "UnitOfWork" => new("Use the scoped Unit of Work as the commit and explicit-transaction coordinator for all resolved repository contexts.", ["Resolve participating repositories before creating a transaction so their contexts are enlisted."], ["Commit timing belongs to the Provider/workflow.", "Database commits do not make remote side effects atomic.", "Dispose transactions through the installed lifecycle and preserve cancellation."], ["paradigm api show IUnitOfWork --project <solution>", "dotnet test <transaction-tests>"]),
        "StoredProcedureBase" or "ResultStoredProcedureBase" => new("Derive the SQL Server or PostgreSQL stored-procedure base that matches whether the routine returns rows, and supply the generated/provider mapper required by that package.", ["Keep the caller and mapper in Data; expose an application-meaningful repository method inward.", "Use the repository/context GetDbConnection() boundary so the command participates in the scoped Unit of Work."], ["Preserve connection ownership: do not dispose a context-owned connection.", "Set and document timeouts; preserve cancellation and deterministic result ordering.", "Resolve every participant before an explicit transaction and verify parameter direction/null mapping."], ["paradigm api show StoredProcedureBase --package SqlServer --project <solution>", "paradigm api show ResultStoredProcedureBase --package PostgreSql --project <solution>", "dotnet test <database-integration-tests>"]),
        _ => null
    };
    private static string Ungeneric(string value)
    {
        var index = value.IndexOf('<');
        return index < 0 ? value : value[..index];
    }
#endregion
}
