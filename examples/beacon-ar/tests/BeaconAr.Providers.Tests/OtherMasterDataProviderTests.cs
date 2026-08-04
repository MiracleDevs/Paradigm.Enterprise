using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Entities;
using AddressType = BeaconAr.Interfaces.MasterData.Enums.AddressType;
using Paradigm.Enterprise.Domain.Exceptions;

namespace BeaconAr.Providers.Tests;

[TestClass]
public sealed class OtherMasterDataProviderTests
{
    #region Public Methods

    [TestMethod]
    public void CustomerOwnsPersistenceShapeValidation()
    {
        Assert.Throws<DomainException>(() => Customer.Create(new CustomerCreateRequest(
            "ACCOUNT", "Customer", "customer@example.test", null, 1.235m, 30), 1, DateTimeOffset.UnixEpoch));
    }

    [TestMethod]
    public void AddressOwnsDefaultCompatibilityValidation()
    {
        Assert.Throws<DomainException>(() => CustomerAddress.Create(new AddressCreateRequest(
            1, "billing", "Address", "Line 1", null, "City", null, "1000", "AR", false, true),
            (int)AddressType.Billing, 1, DateTimeOffset.UnixEpoch));
    }

    [TestMethod]
    public void CarrierOwnsTrackingTemplateValidation()
    {
        Assert.Throws<DomainException>(() => Carrier.Create(new CarrierCreateRequest(
            "CODE", "Carrier", "Ground", "http://carrier.test/{trackingNumber}"), 1, DateTimeOffset.UnixEpoch));
    }

    #endregion
}
