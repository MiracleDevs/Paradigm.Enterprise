using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Interfaces.MasterData.Entities;
using AddressTypeContract = BeaconAr.Interfaces.MasterData.Enums.AddressType;
using Paradigm.Enterprise.Domain.Exceptions;

namespace BeaconAr.Domain.MasterData.Entities;

public partial class CustomerAddress
{
    #region Nested Types

    private sealed record ProposedState(int CustomerId, int AddressTypeId, string Label, string Line1,
        string? Line2, string City, string? State, string PostalCode, string Country,
        bool IsDefaultBilling, bool IsDefaultShipping);

    #endregion

    #region Public Methods

    public static CustomerAddress Create(AddressCreateRequest request, int addressTypeId, int userId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTypeCode(request.Type, addressTypeId);
        ProposedState state = Normalize(request.CustomerId, addressTypeId, request.Label, request.Line1, request.Line2,
            request.City, request.State, request.PostalCode, request.Country, request.DefaultBilling, request.DefaultShipping);
        ValidateState(state);
        return new CustomerAddress
        {
            CustomerId = state.CustomerId,
            AddressTypeId = state.AddressTypeId,
            Label = state.Label,
            Line1 = state.Line1,
            Line2 = state.Line2,
            City = state.City,
            State = state.State,
            PostalCode = state.PostalCode,
            Country = state.Country,
            IsDefaultBilling = state.IsDefaultBilling,
            IsDefaultShipping = state.IsDefaultShipping,
            CreatedByUserId = userId,
            CreationDate = now,
        };
    }

    public void Replace(AddressUpdateRequest request, int addressTypeId, int userId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTypeCode(request.Type, addressTypeId);
        ProposedState state = Normalize(request.CustomerId, addressTypeId, request.Label, request.Line1, request.Line2,
            request.City, request.State, request.PostalCode, request.Country, request.DefaultBilling, request.DefaultShipping);
        ValidateState(state);
        CustomerId = state.CustomerId;
        AddressTypeId = state.AddressTypeId;
        Label = state.Label;
        Line1 = state.Line1;
        Line2 = state.Line2;
        City = state.City;
        State = state.State;
        PostalCode = state.PostalCode;
        Country = state.Country;
        IsDefaultBilling = state.IsDefaultBilling;
        IsDefaultShipping = state.IsDefaultShipping;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    public void ClearBillingDefault(int userId, DateTimeOffset now)
    {
        IsDefaultBilling = false;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    public void ClearShippingDefault(int userId, DateTimeOffset now)
    {
        IsDefaultShipping = false;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    #endregion

    #region Private Methods

    partial void ValidateEntity() => ValidateState(Normalize(CustomerId, AddressTypeId, Label, Line1, Line2, City,
        State, PostalCode, Country, IsDefaultBilling, IsDefaultShipping));

    partial void BeforeMap(ICustomerAddress model)
    {
        _ = Id;
        ValidateState(Normalize(model.CustomerId, model.AddressTypeId, model.Label, model.Line1, model.Line2,
            model.City, model.State, model.PostalCode, model.Country, model.IsDefaultBilling, model.IsDefaultShipping));
    }

    partial void AfterMap(ICustomerAddress model)
    {
        ProposedState state = Normalize(CustomerId, AddressTypeId, Label, Line1, Line2, City, State, PostalCode,
            Country, IsDefaultBilling, IsDefaultShipping);
        Label = state.Label;
        Line1 = state.Line1;
        Line2 = state.Line2;
        City = state.City;
        State = state.State;
        PostalCode = state.PostalCode;
        Country = state.Country;
    }

    private static ProposedState Normalize(int customerId, int addressTypeId, string? label, string? line1,
        string? line2, string? city, string? state, string? postalCode, string? country,
        bool defaultBilling, bool defaultShipping) => new(
        customerId,
        addressTypeId,
        label?.Trim() ?? string.Empty,
        line1?.Trim() ?? string.Empty,
        string.IsNullOrWhiteSpace(line2) ? null : line2.Trim(),
        city?.Trim() ?? string.Empty,
        string.IsNullOrWhiteSpace(state) ? null : state.Trim(),
        postalCode?.Trim() ?? string.Empty,
        (country?.Trim() ?? string.Empty).ToUpperInvariant(),
        defaultBilling,
        defaultShipping);

    private static void ValidateState(ProposedState state)
    {
        var rules = new DomainValidator();
        rules.Assert(state.CustomerId > 0, "Customer ID must be greater than zero.");
        rules.Assert(Enum.IsDefined((AddressTypeContract)state.AddressTypeId), "Address type ID is invalid.");
        rules.Assert(state.Label.Length is >= 1 and <= 120, "Label is required and cannot exceed 120 characters.");
        rules.Assert(state.Line1.Length is >= 1 and <= 200, "Address line 1 is required and cannot exceed 200 characters.");
        rules.Assert(state.Line2 is null || state.Line2.Length <= 200, "Address line 2 cannot exceed 200 characters.");
        rules.Assert(state.City.Length is >= 1 and <= 120, "City is required and cannot exceed 120 characters.");
        rules.Assert(state.State is null || state.State.Length <= 120, "State cannot exceed 120 characters.");
        rules.Assert(state.PostalCode.Length is >= 1 and <= 32, "Postal code is required and cannot exceed 32 characters.");
        rules.Assert(state.Country.Length == 2 && state.Country.All(character => character is >= 'A' and <= 'Z'),
            "Country must be a two-letter code.");
        rules.Assert(!state.IsDefaultBilling || state.AddressTypeId is (int)AddressTypeContract.Billing or (int)AddressTypeContract.Both,
            "A billing default must have billing or both type.");
        rules.Assert(!state.IsDefaultShipping || state.AddressTypeId is (int)AddressTypeContract.Shipping or (int)AddressTypeContract.Both,
            "A shipping default must have shipping or both type.");
        rules.ThrowIfAny();
    }

    private static void ValidateTypeCode(string? type, int addressTypeId)
    {
        string code = type?.Trim().ToLowerInvariant() ?? string.Empty;
        int expectedId = code switch
        {
            "billing" => (int)AddressTypeContract.Billing,
            "shipping" => (int)AddressTypeContract.Shipping,
            "both" => (int)AddressTypeContract.Both,
            _ => 0,
        };
        var rules = new DomainValidator();
        rules.Assert(expectedId != 0, "Address type must be billing, shipping, or both.");
        rules.Assert(expectedId == addressTypeId, "Address type ID and code must identify the same catalog value.");
        rules.ThrowIfAny();
    }

    #endregion
}
