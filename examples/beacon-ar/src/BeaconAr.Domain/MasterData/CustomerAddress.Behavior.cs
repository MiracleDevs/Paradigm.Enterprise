using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Validation;

namespace BeaconAr.Domain.Receivables.Entities;

public partial class CustomerAddress
{
    #region Public Methods

    public static CustomerAddress Create(AddressCreateRequest request, int addressTypeId, int userId, DateTimeOffset now)
    {
        AddressCreateRequest value = MasterDataRequestValidator.Normalize(request);
        return new CustomerAddress
        {
            CustomerId = value.CustomerId,
            AddressTypeId = addressTypeId,
            Label = value.Label!,
            Line1 = value.Line1!,
            Line2 = value.Line2,
            City = value.City!,
            State = value.State,
            PostalCode = value.PostalCode!,
            Country = value.Country!,
            IsDefaultBilling = value.DefaultBilling,
            IsDefaultShipping = value.DefaultShipping,
            CreatedByUserId = userId,
            CreationDate = now,
        };
    }

    public void Replace(AddressUpdateRequest request, int addressTypeId, int userId, DateTimeOffset now)
    {
        AddressUpdateRequest value = MasterDataRequestValidator.Normalize(request);
        CustomerId = value.CustomerId;
        AddressTypeId = addressTypeId;
        Label = value.Label!;
        Line1 = value.Line1!;
        Line2 = value.Line2;
        City = value.City!;
        State = value.State;
        PostalCode = value.PostalCode!;
        Country = value.Country!;
        IsDefaultBilling = value.DefaultBilling;
        IsDefaultShipping = value.DefaultShipping;
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
}
