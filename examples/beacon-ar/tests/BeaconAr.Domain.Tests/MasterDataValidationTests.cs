using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Validation;
using BeaconAr.Domain.Receivables.Generated;

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

        Assert.Throws<MasterDataValidationException>(() => product.Replace(
            new ProductUpdateRequest("X", "Changed", "Changed", 0, -1, "ftp://unsafe.test/file", false),
            2, DateTimeOffset.UnixEpoch.AddDays(1)));

        Assert.AreEqual("SKU", product.Sku);
        Assert.AreEqual("Desk", product.Name);
        Assert.AreEqual(10m, product.UnitPrice);
        Assert.IsTrue(product.IsActive);
        Assert.IsNull(product.ModificationDate);
    }

    [TestMethod]
    public void ProductPriceMatchesSqlDecimalPrecisionAndScale()
    {
        ProductCreateRequest exactMaximum = MasterDataRequestValidator.Normalize(new ProductCreateRequest(
            "SKU", "Product", "Category", 999999999999999.9999m, 0, null));

        Assert.AreEqual(999999999999999.9999m, exactMaximum.UnitPrice);
        Assert.IsTrue(Assert.Throws<MasterDataValidationException>(() => MasterDataRequestValidator.Normalize(
            exactMaximum with { UnitPrice = 1000000000000000m })).Errors.ContainsKey("unitPrice"));
        Assert.IsTrue(Assert.Throws<MasterDataValidationException>(() => MasterDataRequestValidator.Normalize(
            exactMaximum with { UnitPrice = 1.23456m })).Errors.ContainsKey("unitPrice"));
    }

    [TestMethod]
    public void CustomerCreditLimitMatchesSqlDecimalPrecisionAndScale()
    {
        CustomerCreateRequest exactMaximum = MasterDataRequestValidator.Normalize(new CustomerCreateRequest(
            "ACCOUNT", "Customer", "person@example.test", null, 99999999999999999.99m, 30));

        Assert.AreEqual(99999999999999999.99m, exactMaximum.CreditLimit);
        Assert.IsTrue(Assert.Throws<MasterDataValidationException>(() => MasterDataRequestValidator.Normalize(
            exactMaximum with { CreditLimit = 100000000000000000m })).Errors.ContainsKey("creditLimit"));
        Assert.IsTrue(Assert.Throws<MasterDataValidationException>(() => MasterDataRequestValidator.Normalize(
            exactMaximum with { CreditLimit = 1.235m })).Errors.ContainsKey("creditLimit"));
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
    public void CustomerRejectsInvalidEmailTermsAndCreditLimitWithFieldPaths()
    {
        MasterDataValidationException exception = Assert.Throws<MasterDataValidationException>(() =>
            MasterDataRequestValidator.Normalize(new CustomerCreateRequest("A", "Customer", "not an email", null, -1, 10)));

        Assert.IsTrue(exception.Errors.ContainsKey("email"));
        Assert.IsTrue(exception.Errors.ContainsKey("creditLimit"));
        Assert.IsTrue(exception.Errors.ContainsKey("paymentTermsDays"));
    }

    [TestMethod]
    [DataRow("billing", true, false)]
    [DataRow("shipping", false, true)]
    [DataRow("both", true, true)]
    public void AddressAcceptsCompatibleDefaultTypes(string type, bool billing, bool shipping)
    {
        AddressCreateRequest result = MasterDataRequestValidator.Normalize(new AddressCreateRequest(
            1, type, " Main ", " Street ", null, " City ", null, " 1000 ", " ar ", billing, shipping));

        Assert.AreEqual("AR", result.Country);
        Assert.AreEqual(type, result.Type);
    }

    [TestMethod]
    public void AddressRejectsIncompatibleDefaultType()
    {
        MasterDataValidationException exception = Assert.Throws<MasterDataValidationException>(() =>
            MasterDataRequestValidator.Normalize(new AddressCreateRequest(
                1, "billing", "Main", "Street", null, "City", null, "1000", "AR", false, true)));

        Assert.IsTrue(exception.Errors.ContainsKey("defaultShipping"));
    }

    [TestMethod]
    public void SafeUrlsRejectCredentialsControlsAndUnknownPlaceholders()
    {
        Assert.IsTrue(SafeUrlValidator.IsSafeThumbnail("http://example.test/image.png"));
        Assert.IsFalse(SafeUrlValidator.IsSafeThumbnail("https://user:password@example.test/image.png"));
        Assert.IsFalse(SafeUrlValidator.IsSafeThumbnail("https://example.test/{image}"));
        Assert.IsFalse(SafeUrlValidator.IsSafeThumbnail("https://example.test/a\nimage"));
        Assert.IsTrue(SafeUrlValidator.IsSafeTrackingTemplate("https://carrier.test/track/{trackingNumber}"));
        Assert.IsTrue(SafeUrlValidator.IsSafeTrackingTemplate("https://carrier.test/track"));
        Assert.IsFalse(SafeUrlValidator.IsSafeTrackingTemplate("http://carrier.test/track/{trackingNumber}"));
        Assert.IsFalse(SafeUrlValidator.IsSafeTrackingTemplate("https://carrier.test/{other}"));
    }

    [TestMethod]
    public void VersionCodecRequiresCanonicalEightByteBase64()
    {
        byte[] version = [1, 2, 3, 4, 5, 6, 7, 8];
        string encoded = VersionTokenCodec.Encode(version);

        CollectionAssert.AreEqual(version, VersionTokenCodec.Decode(encoded));
        Assert.Throws<MasterDataValidationException>(() => VersionTokenCodec.Decode("AQID"));
        Assert.Throws<MasterDataValidationException>(() => VersionTokenCodec.Decode("not-base64"));
        Assert.Throws<MasterDataValidationException>(() => VersionTokenCodec.Decode(null));
    }

    [TestMethod]
    public void SearchValidationRejectsUnsafePagingAndUnknownSorts()
    {
        MasterDataValidationException exception = Assert.Throws<MasterDataValidationException>(() =>
            MasterDataRequestValidator.ValidateSearch(new ProductSearchRequest
            {
                PageNumber = 0,
                PageSize = 101,
                SortField = "drop table",
            }, "id", "sku", "name"));

        Assert.IsTrue(exception.Errors.ContainsKey("pageNumber"));
        Assert.IsTrue(exception.Errors.ContainsKey("pageSize"));
        Assert.IsTrue(exception.Errors.ContainsKey("sortField"));
    }

    [TestMethod]
    public void SearchValidationAcceptsMaximumPageAndSearchLengthButRejectsOverLengthSearch()
    {
        MasterDataRequestValidator.ValidateSearch(new ProductSearchRequest
        {
            PageNumber = int.MaxValue,
            PageSize = 100,
            Search = new string('x', 320),
        }, "id", "sku", "name");

        MasterDataValidationException exception = Assert.Throws<MasterDataValidationException>(() =>
            MasterDataRequestValidator.ValidateSearch(new ProductSearchRequest
            {
                Search = new string('x', 321),
            }, "id", "sku", "name"));
        Assert.IsTrue(exception.Errors.ContainsKey("search"));
    }

    #endregion
}
