using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Providers.MasterData;
using System.Reflection;

namespace BeaconAr.Providers.Tests;

[TestClass]
public sealed class OtherMasterDataProviderTests
{
    #region Nested Types

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1852", Justification = "DispatchProxy requires an inheritable proxy type.")]
    private class ThrowingDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("A validation-first test used an unexpected collaborator.");
    }

    private sealed class StubServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => DispatchProxy.Create(serviceType, typeof(ThrowingDispatchProxy));
    }

    #endregion

    #region Public Methods

    [TestMethod]
    public async Task CustomerRejectsInvalidPersistenceShapeBeforeUsingCollaborators()
    {
        var provider = new CustomerProvider(new StubServiceProvider(), CreateCoordinator());

        MasterDataValidationException exception = await Assert.ThrowsAsync<MasterDataValidationException>(() =>
            provider.CreateAsync(new CustomerCreateRequest(
                "ACCOUNT", "Customer", "customer@example.test", null, 1.235m, 30), CancellationToken.None));

        Assert.IsTrue(exception.Errors.ContainsKey("creditLimit"));
    }

    [TestMethod]
    public async Task AddressRejectsInvalidDefaultBeforeUsingCollaborators()
    {
        var services = new StubServiceProvider();
        var provider = new AddressProvider(services,
            (ICustomerRepository)services.GetService(typeof(ICustomerRepository))!, CreateCoordinator());

        MasterDataValidationException exception = await Assert.ThrowsAsync<MasterDataValidationException>(() =>
            provider.CreateAsync(new AddressCreateRequest(
                1, "billing", "Address", "Line 1", null, "City", null, "1000", "AR", false, true),
                CancellationToken.None));

        Assert.IsTrue(exception.Errors.ContainsKey("defaultShipping"));
    }

    [TestMethod]
    public async Task CarrierRejectsUnsafeTrackingTemplateBeforeUsingCollaborators()
    {
        var provider = new CarrierProvider(new StubServiceProvider(), CreateCoordinator());

        MasterDataValidationException exception = await Assert.ThrowsAsync<MasterDataValidationException>(() =>
            provider.CreateAsync(new CarrierCreateRequest(
                "CODE", "Carrier", "Ground", "http://carrier.test/{trackingNumber}"), CancellationToken.None));

        Assert.IsTrue(exception.Errors.ContainsKey("trackingUrlTemplate"));
    }

    #endregion

    #region Private Methods

    private static MasterDataMutationCoordinator CreateCoordinator() => new(null!, null!, null!, null!, null!, null!);

    #endregion
}
