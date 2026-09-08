using Paradigm.Enterprise.Domain.Dtos;
using BeaconAr.Domain.MasterData.Application;

namespace BeaconAr.Domain.MasterData.Contracts;

public sealed class MasterDataViewSearchParameters : PaginationParametersBase
{
    #region Properties

    public bool? Active { get; set; }

    public int? CustomerId { get; set; }

    public string? Search { get; set; }

    public string? Type { get; set; }

    public AddressUsage? Usage { get; set; }

    #endregion

    #region Public Methods

    public void Validate(params string[] allowedSortFields)
    {
        Dictionary<string, IReadOnlyList<string>> errors = new(StringComparer.Ordinal);
        int pageNumber = PageNumber ?? 1;
        int pageSize = PageSize ?? 10;
        if (pageNumber <= 0)
            errors["pageNumber"] = ["Page number must be greater than zero."];
        if (pageSize is <= 0 or > 100)
            errors["pageSize"] = ["Page size must be between 1 and 100."];
        if (!string.IsNullOrWhiteSpace(SortDirection) &&
            !SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) &&
            !SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
            errors["sortDirection"] = ["Sort direction must be asc or desc."];
        if (Search?.Trim().Length > 320)
            errors["search"] = ["Search cannot exceed 320 characters."];

        string sortField = string.IsNullOrWhiteSpace(SortBy) ? "id" : SortBy.Trim();
        if (!allowedSortFields.Contains(sortField, StringComparer.OrdinalIgnoreCase))
            errors["sortField"] = ["Sort field is not supported."];
        if (CustomerId is <= 0)
            errors["customerId"] = ["Customer ID must be greater than zero."];
        string? type = string.IsNullOrWhiteSpace(Type) ? null : Type.Trim();
        if (type is { Length: > 32 } || type?.Any(char.IsControl) == true)
            errors["type"] = ["Address type cannot exceed 32 characters or contain control characters."];
        if (Usage.HasValue && !Enum.IsDefined(Usage.Value))
            errors["usage"] = ["Address usage must be billing or shipping."];

        if (errors.Count > 0)
            throw new MasterDataValidationException(errors);
    }

    #endregion
}
