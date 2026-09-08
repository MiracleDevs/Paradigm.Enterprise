namespace BeaconAr.CodeGenerator;

internal static class GenerationTargets
{
    #region Public Methods

    public static IReadOnlySet<GenerationTarget> Parse(IReadOnlyCollection<string> arguments)
    {
        if (arguments.Count == 0)
        {
            return new HashSet<GenerationTarget>
            {
                GenerationTarget.JsonContexts,
                GenerationTarget.StoredProcedureMappers,
            };
        }

        var targets = new HashSet<GenerationTarget>();
        foreach (var argument in arguments)
        {
            switch (argument.ToLowerInvariant())
            {
                case "json":
                    targets.Add(GenerationTarget.JsonContexts);
                    break;
                case "mappers":
                    targets.Add(GenerationTarget.StoredProcedureMappers);
                    break;
                default:
                    throw new ArgumentException($"Unknown generation target '{argument}'. Use 'json' or 'mappers'.", nameof(arguments));
            }
        }

        return targets;
    }

    #endregion
}
