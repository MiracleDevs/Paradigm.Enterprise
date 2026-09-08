namespace BeaconAr.Domain.MasterData.Contracts;

public sealed class AddressSearchRequest : MasterDataSearchRequest
{
    #region Properties

    public int? CustomerId { get; init; }

    public string? Type { get; init; }

    public AddressUsage? Usage { get; init; }

    #endregion

    #region Overrides

    protected override void AddValidationErrors(IDictionary<string, IReadOnlyList<string>> errors)
    {
        if (CustomerId is <= 0)
            errors["customerId"] = ["Customer ID must be greater than zero."];
        string? type = string.IsNullOrWhiteSpace(Type) ? null : Type.Trim();
        if (type is { Length: > 32 } || type?.Any(char.IsControl) == true)
            errors["type"] = ["Address type cannot exceed 32 characters or contain control characters."];
        if (Usage.HasValue && !Enum.IsDefined(Usage.Value))
            errors["usage"] = ["Address usage must be billing or shipping."];
    }

    #endregion
}
