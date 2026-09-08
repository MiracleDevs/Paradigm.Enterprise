using BeaconAr.Domain.MasterData.Application;

namespace BeaconAr.Domain.MasterData.Contracts;

public abstract class MasterDataSearchRequest
{
    #region Properties

    public string? Search { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? SortField { get; init; }

    public SortDirection SortDirection { get; init; } = SortDirection.Asc;

    #endregion

    #region Public Methods

    public void Validate(params string[] allowedSortFields)
    {
        Dictionary<string, IReadOnlyList<string>> errors = new(StringComparer.Ordinal);
        if (PageNumber <= 0)
            errors["pageNumber"] = ["Page number must be greater than zero."];
        if (PageSize is <= 0 or > 100)
            errors["pageSize"] = ["Page size must be between 1 and 100."];
        if (!Enum.IsDefined(SortDirection))
            errors["sortDirection"] = ["Sort direction must be asc or desc."];
        if (Search?.Trim().Length > 320)
            errors["search"] = ["Search cannot exceed 320 characters."];

        string sortField = string.IsNullOrWhiteSpace(SortField) ? "id" : SortField.Trim();
        if (!allowedSortFields.Contains(sortField, StringComparer.OrdinalIgnoreCase))
            errors["sortField"] = ["Sort field is not supported."];

        AddValidationErrors(errors);
        if (errors.Count > 0)
            throw new MasterDataValidationException(errors);
    }

    #endregion

    #region Protected Methods

    protected virtual void AddValidationErrors(IDictionary<string, IReadOnlyList<string>> errors)
    {
    }

    #endregion
}
