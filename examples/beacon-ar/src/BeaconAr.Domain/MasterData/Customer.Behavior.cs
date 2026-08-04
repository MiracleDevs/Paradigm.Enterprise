using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Validation;

namespace BeaconAr.Domain.Receivables.Entities;

public partial class Customer
{
    #region Properties

    public ICollection<CustomerAddress> CustomerAddresses { get; set; } = new List<CustomerAddress>();

    #endregion

    #region Public Methods

    public static Customer Create(CustomerCreateRequest request, int userId, DateTimeOffset now)
    {
        CustomerCreateRequest value = MasterDataRequestValidator.Normalize(request);
        return new Customer
        {
            AccountNumber = value.AccountNumber!,
            Name = value.Name!,
            Email = value.Email!,
            Phone = value.Phone,
            CreditLimit = value.CreditLimit,
            PaymentTermsDays = value.PaymentTermsDays,
            IsActive = value.IsActive,
            CreatedByUserId = userId,
            CreationDate = now,
        };
    }

    public void Replace(CustomerUpdateRequest request, int userId, DateTimeOffset now)
    {
        CustomerUpdateRequest value = MasterDataRequestValidator.Normalize(request);
        AccountNumber = value.AccountNumber!;
        Name = value.Name!;
        Email = value.Email!;
        Phone = value.Phone;
        CreditLimit = value.CreditLimit;
        PaymentTermsDays = value.PaymentTermsDays;
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
