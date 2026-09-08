using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Access.Entities;
using BeaconAr.Domain.MasterData.Entities;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Entities;
using BeaconAr.Domain.Sales.Entities;
using AddressType = BeaconAr.Interfaces.MasterData.Enums.AddressType;
using Paradigm.Enterprise.Domain.Exceptions;

namespace BeaconAr.Domain.Tests;

[TestClass]
public sealed class MasterDataValidationTests
{
    #region Public Methods

    [TestMethod]
    public void ProductNormalizationAndBehaviorPreserveUserCasing()
    {
        Product product = Product.Create(new ProductCreateRequest(
            "  Sku-1  ", "  Desk  ", " Furniture ", 10.5m, 0, " https://example.test/image.png "),
            7, DateTimeOffset.UnixEpoch);

        Assert.AreEqual("Sku-1", product.Sku);
        Assert.AreEqual("Desk", product.Name);
        Assert.AreEqual("Furniture", product.Category);
        Assert.AreEqual("https://example.test/image.png", product.ThumbnailUrl);
        Assert.AreEqual(7, product.CreatedByUserId);
    }

    [TestMethod]
    public void FailedProductReplacementIsAtomic()
    {
        Product product = Product.Create(new ProductCreateRequest("SKU", "Desk", "Furniture", 10m, 2, null), 1, DateTimeOffset.UnixEpoch);

        Assert.Throws<DomainException>(() => product.Replace(
            new ProductUpdateRequest("X", "Changed", "Changed", 0, -1, "ftp://unsafe.test/file", false),
            2, DateTimeOffset.UnixEpoch.AddDays(1)));

        Assert.AreEqual("SKU", product.Sku);
        Assert.AreEqual("Desk", product.Name);
        Assert.AreEqual(10m, product.UnitPrice);
        Assert.IsTrue(product.IsActive);
        Assert.IsNull(product.ModificationDate);
    }

    [TestMethod]
    public void FailedCustomerReplacementIsAtomic()
    {
        Customer customer = Customer.Create(new CustomerCreateRequest(
            "ACCOUNT", "Customer", "person@example.test", "123", 10m, 30), 1, DateTimeOffset.UnixEpoch);

        Assert.Throws<DomainException>(() => customer.Replace(new CustomerUpdateRequest(
            "CHANGED", "Changed", "not-an-email", "456", -1m, 10, false), 2,
            DateTimeOffset.UnixEpoch.AddDays(1)));

        Assert.AreEqual("ACCOUNT", customer.AccountNumber);
        Assert.AreEqual("Customer", customer.Name);
        Assert.AreEqual("person@example.test", customer.Email);
        Assert.AreEqual("123", customer.Phone);
        Assert.AreEqual(10m, customer.CreditLimit);
        Assert.AreEqual((short)30, customer.PaymentTermsDays);
        Assert.IsTrue(customer.IsActive);
        Assert.IsNull(customer.ModifiedByUserId);
        Assert.IsNull(customer.ModificationDate);
    }

    [TestMethod]
    public void FailedAddressReplacementIsAtomic()
    {
        CustomerAddress address = CustomerAddress.Create(new AddressCreateRequest(
            1, "both", "Main", "Street", null, "City", null, "1000", "AR", true, true),
            (int)AddressType.Both, 1, DateTimeOffset.UnixEpoch);

        Assert.Throws<DomainException>(() => address.Replace(new AddressUpdateRequest(
            2, "billing", "Changed", "Changed", "Suite", "Changed", "State", "2000", "US", false, true),
            (int)AddressType.Billing, 2, DateTimeOffset.UnixEpoch.AddDays(1)));

        Assert.AreEqual(1, address.CustomerId);
        Assert.AreEqual((int)AddressType.Both, address.AddressTypeId);
        Assert.AreEqual("Main", address.Label);
        Assert.AreEqual("Street", address.Line1);
        Assert.AreEqual("City", address.City);
        Assert.AreEqual("1000", address.PostalCode);
        Assert.AreEqual("AR", address.Country);
        Assert.IsTrue(address.IsDefaultBilling);
        Assert.IsTrue(address.IsDefaultShipping);
        Assert.IsNull(address.ModifiedByUserId);
        Assert.IsNull(address.ModificationDate);
    }

    [TestMethod]
    public void FailedCarrierReplacementIsAtomic()
    {
        Carrier carrier = Carrier.Create(new CarrierCreateRequest(
            "C", "Carrier", "Ground", "https://carrier.test/{trackingNumber}"), 1, DateTimeOffset.UnixEpoch);

        Assert.Throws<DomainException>(() => carrier.Replace(new CarrierUpdateRequest(
            "CHANGED", "Changed", "Air", "http://unsafe.test/{trackingNumber}", false), 2,
            DateTimeOffset.UnixEpoch.AddDays(1)));

        Assert.AreEqual("C", carrier.Code);
        Assert.AreEqual("Carrier", carrier.Name);
        Assert.AreEqual("Ground", carrier.ServiceLevel);
        Assert.AreEqual("https://carrier.test/{trackingNumber}", carrier.TrackingUrlTemplate);
        Assert.IsTrue(carrier.IsActive);
        Assert.IsNull(carrier.ModifiedByUserId);
        Assert.IsNull(carrier.ModificationDate);
    }

    [TestMethod]
    public void ProductPriceMatchesSqlDecimalPrecisionAndScale()
    {
        Product exactMaximum = Product.Create(new ProductCreateRequest(
            "SKU", "Product", "Category", 999999999999999.9999m, 0, null), 1, DateTimeOffset.UnixEpoch);

        Assert.AreEqual(999999999999999.9999m, exactMaximum.UnitPrice);
        Assert.Throws<DomainException>(() => Product.Create(new ProductCreateRequest(
            "SKU", "Product", "Category", 1000000000000000m, 0, null), 1, DateTimeOffset.UnixEpoch));
        Assert.Throws<DomainException>(() => Product.Create(new ProductCreateRequest(
            "SKU", "Product", "Category", 1.23456m, 0, null), 1, DateTimeOffset.UnixEpoch));
    }

    [TestMethod]
    public void CustomerCreditLimitMatchesSqlDecimalPrecisionAndScale()
    {
        Customer exactMaximum = Customer.Create(new CustomerCreateRequest(
            "ACCOUNT", "Customer", "person@example.test", null, 99999999999999999.99m, 30), 1, DateTimeOffset.UnixEpoch);

        Assert.AreEqual(99999999999999999.99m, exactMaximum.CreditLimit);
        Assert.Throws<DomainException>(() => Customer.Create(new CustomerCreateRequest(
            "ACCOUNT", "Customer", "person@example.test", null, 100000000000000000m, 30), 1, DateTimeOffset.UnixEpoch));
        Assert.Throws<DomainException>(() => Customer.Create(new CustomerCreateRequest(
            "ACCOUNT", "Customer", "person@example.test", null, 1.235m, 30), 1, DateTimeOffset.UnixEpoch));
    }

    [TestMethod]
    [DataRow((short)0)]
    [DataRow((short)15)]
    [DataRow((short)30)]
    [DataRow((short)45)]
    [DataRow((short)60)]
    public void CustomerAcceptsDocumentedPaymentTerms(short terms)
    {
        Customer customer = Customer.Create(new CustomerCreateRequest(
            "A-1", "Customer", "person@example.test", null, 0, terms), 1, DateTimeOffset.UnixEpoch);

        Assert.AreEqual(terms, customer.PaymentTermsDays);
    }

    [TestMethod]
    public void CustomerRejectsInvalidEmailTermsAndCreditLimit()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Customer.Create(
            new CustomerCreateRequest("A", "Customer", "not an email", null, -1, 10), 1, DateTimeOffset.UnixEpoch));
        StringAssert.Contains(exception.Message, "Email");
        StringAssert.Contains(exception.Message, "Credit limit");
        StringAssert.Contains(exception.Message, "Payment terms");
    }

    [TestMethod]
    [DataRow("billing", true, false)]
    [DataRow("shipping", false, true)]
    [DataRow("both", true, true)]
    public void AddressAcceptsCompatibleDefaultTypes(string type, bool billing, bool shipping)
    {
        int typeId = type switch { "billing" => (int)AddressType.Billing, "shipping" => (int)AddressType.Shipping, _ => (int)AddressType.Both };
        CustomerAddress result = CustomerAddress.Create(new AddressCreateRequest(
            1, type, " Main ", " Street ", null, " City ", null, " 1000 ", " ar ", billing, shipping),
            typeId, 1, DateTimeOffset.UnixEpoch);

        Assert.AreEqual("AR", result.Country);
        Assert.AreEqual(typeId, result.AddressTypeId);
    }

    [TestMethod]
    public void AddressRejectsIncompatibleDefaultType()
    {
        Assert.Throws<DomainException>(() => CustomerAddress.Create(new AddressCreateRequest(
            1, "billing", "Main", "Street", null, "City", null, "1000", "AR", false, true),
            (int)AddressType.Billing, 1, DateTimeOffset.UnixEpoch));
    }

    [TestMethod]
    public void SafeUrlsRejectCredentialsControlsAndUnknownPlaceholders()
    {
        _ = Product.Create(new ProductCreateRequest("SKU", "Product", "Category", 1m, 0,
            "http://example.test/image.png"), 1, DateTimeOffset.UnixEpoch);
        Assert.Throws<DomainException>(() => Product.Create(new ProductCreateRequest("SKU", "Product", "Category", 1m, 0,
            "https://user:password@example.test/image.png"), 1, DateTimeOffset.UnixEpoch));
        Assert.Throws<DomainException>(() => Product.Create(new ProductCreateRequest("SKU", "Product", "Category", 1m, 0,
            "https://example.test/{image}"), 1, DateTimeOffset.UnixEpoch));
        _ = Carrier.Create(new CarrierCreateRequest("C", "Carrier", "Ground",
            "https://carrier.test/track/{trackingNumber}"), 1, DateTimeOffset.UnixEpoch);
        Assert.Throws<DomainException>(() => Carrier.Create(new CarrierCreateRequest("C", "Carrier", "Ground",
            "http://carrier.test/track/{trackingNumber}"), 1, DateTimeOffset.UnixEpoch));
    }

    [TestMethod]
    public void VersionCodecRequiresCanonicalEightByteBase64()
    {
        byte[] version = [1, 2, 3, 4, 5, 6, 7, 8];
        string encoded = VersionTokenCodec.Encode(version);

        CollectionAssert.AreEqual(version, VersionTokenCodec.Decode(encoded));
        Assert.Throws<VersionTokenException>(() => VersionTokenCodec.Decode("AQID"));
        Assert.Throws<VersionTokenException>(() => VersionTokenCodec.Decode("not-base64"));
        Assert.Throws<VersionTokenException>(() => VersionTokenCodec.Decode(null));
    }

    [TestMethod]
    public void SearchValidationRejectsUnsafePagingAndUnknownSorts()
    {
        MasterDataValidationException exception = Assert.Throws<MasterDataValidationException>(() =>
            new ProductSearchRequest
            {
                PageNumber = 0,
                PageSize = 101,
                SortField = "drop table",
            }.Validate("id", "sku", "name"));

        Assert.IsTrue(exception.Errors.ContainsKey("pageNumber"));
        Assert.IsTrue(exception.Errors.ContainsKey("pageSize"));
        Assert.IsTrue(exception.Errors.ContainsKey("sortField"));
    }

    [TestMethod]
    public void SearchValidationAcceptsMaximumPageAndSearchLengthButRejectsOverLengthSearch()
    {
        new ProductSearchRequest
        {
            PageNumber = int.MaxValue,
            PageSize = 100,
            Search = new string('x', 320),
        }.Validate("id", "sku", "name");

        MasterDataValidationException exception = Assert.Throws<MasterDataValidationException>(() =>
            new ProductSearchRequest
            {
                Search = new string('x', 321),
            }.Validate("id", "sku", "name"));
        Assert.IsTrue(exception.Errors.ContainsKey("search"));
    }

    #endregion
}
