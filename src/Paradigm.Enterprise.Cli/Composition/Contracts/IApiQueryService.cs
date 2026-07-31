namespace Paradigm.Enterprise.Cli;

internal interface IApiQueryService
{
    IReadOnlyList<ResultItem> Search(IEnumerable<InspectedType> types, string query, string? package, int limit);
    ApiMatch Show(IEnumerable<InspectedType> types, string symbol, string? package);
}