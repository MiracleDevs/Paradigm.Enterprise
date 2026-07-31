namespace Paradigm.Enterprise.Cli;
internal sealed record ValidationContext(IReadOnlyList<InspectedType> ApplicationTypes, IReadOnlyList<InspectedType> AllTypes);
