using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Validation;

namespace BeaconAr.Domain.Receivables.Generated;

public partial class Carrier
{
    #region Public Methods

    public static Carrier Create(CarrierCreateRequest request, int userId, DateTimeOffset now)
    {
        CarrierCreateRequest value = MasterDataRequestValidator.Normalize(request);
        return new Carrier
        {
            Code = value.Code!,
            Name = value.Name!,
            ServiceLevel = value.ServiceLevel!,
            TrackingUrlTemplate = value.TrackingUrlTemplate,
            IsActive = value.IsActive,
            CreatedByUserId = userId,
            CreationDate = now,
        };
    }

    public void Replace(CarrierUpdateRequest request, int userId, DateTimeOffset now)
    {
        CarrierUpdateRequest value = MasterDataRequestValidator.Normalize(request);
        Code = value.Code!;
        Name = value.Name!;
        ServiceLevel = value.ServiceLevel!;
        TrackingUrlTemplate = value.TrackingUrlTemplate;
        IsActive = value.IsActive;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    public void Activate(int userId, DateTimeOffset now)
    {
        IsActive = true;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    public void Deactivate(int userId, DateTimeOffset now)
    {
        IsActive = false;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    #endregion
}
