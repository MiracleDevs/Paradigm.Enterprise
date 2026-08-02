using System.Text.Json;

namespace BeaconAr.WebApi.Tests;

[TestClass]
public sealed class OpenApiArtifactTests
{
    #region Constants

    private const int ExpectedOperationCount = 36;
    private const int ExpectedPathCount = 18;

    #endregion

    #region Fields

    private static readonly string[] ProblemRequiredProperties = ["status", "code", "title", "detail", "correlationId"];
    private static readonly string[] RootResponseProperties = ["name", "version"];

    private static readonly HashSet<string> BadRequestOperations = new(StringComparer.Ordinal)
    {
        "searchProducts", "getProduct", "createProduct", "updateProduct", "deleteProduct",
        "searchCustomers", "getCustomer", "createCustomer", "updateCustomer", "deleteCustomer",
        "searchAddresses", "getAddress", "createAddress", "updateAddress", "deleteAddress",
        "searchCarriers", "getCarrier", "createCarrier", "updateCarrier", "deleteCarrier",
        "searchQuotes", "getQuote", "createQuote", "updateQuote", "deleteQuote", "transitionQuoteStatus", "convertQuoteToSalesOrder",
        "searchSalesOrders", "getSalesOrder", "createSalesOrder", "updateSalesOrder", "deleteSalesOrder", "transitionSalesOrderStatus",
    };

    private static readonly HashSet<string> NotFoundOperations = new(StringComparer.Ordinal)
    {
        "getProduct", "updateProduct", "deleteProduct",
        "getCustomer", "updateCustomer", "deleteCustomer",
        "getAddress", "createAddress", "updateAddress", "deleteAddress",
        "getCarrier", "updateCarrier", "deleteCarrier",
        "getQuote", "updateQuote", "deleteQuote", "transitionQuoteStatus", "convertQuoteToSalesOrder",
        "getSalesOrder", "updateSalesOrder", "deleteSalesOrder", "transitionSalesOrderStatus",
    };

    private static readonly HashSet<string> ConflictOperations = new(StringComparer.Ordinal)
    {
        "createProduct", "updateProduct", "deleteProduct",
        "createCustomer", "updateCustomer", "deleteCustomer",
        "createAddress", "updateAddress", "deleteAddress",
        "createCarrier", "updateCarrier", "deleteCarrier",
        "createQuote", "updateQuote", "deleteQuote", "transitionQuoteStatus", "convertQuoteToSalesOrder",
        "createSalesOrder", "updateSalesOrder", "deleteSalesOrder", "transitionSalesOrderStatus",
    };

    private static readonly HashSet<string> JsonBodyOperations = new(StringComparer.Ordinal)
    {
        "createProduct", "updateProduct", "createCustomer", "updateCustomer", "createAddress", "updateAddress",
        "createCarrier", "updateCarrier", "createQuote", "updateQuote", "transitionQuoteStatus",
        "createSalesOrder", "updateSalesOrder", "transitionSalesOrderStatus",
    };

    private static readonly HashSet<string> IfMatchOperations = new(StringComparer.Ordinal)
    {
        "updateProduct", "deleteProduct", "updateCustomer", "deleteCustomer", "updateAddress", "deleteAddress",
        "updateCarrier", "deleteCarrier", "updateQuote", "deleteQuote", "transitionQuoteStatus",
        "updateSalesOrder", "deleteSalesOrder", "transitionSalesOrderStatus",
    };

    #endregion

    #region Public Methods

    [TestMethod]
    public void GeneratedDocumentIsACompleteClientContract()
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(FindArtifact()));
        JsonElement root = document.RootElement;
        JsonElement paths = root.GetProperty("paths");
        List<JsonElement> operations = GetOperations(paths);

        Assert.AreEqual(ExpectedPathCount, paths.EnumerateObject().Count());
        Assert.AreEqual(ExpectedOperationCount, operations.Count);
        Assert.AreEqual(ExpectedOperationCount, operations.Select(OperationId).Distinct(StringComparer.Ordinal).Count());
        StringAssert.Contains(root.GetProperty("info").GetProperty("description").GetString()!, "Delegated Microsoft Entra user access tokens");
        string bearerDescription = root.GetProperty("components").GetProperty("securitySchemes")
            .GetProperty("bearerAuth").GetProperty("description").GetString()!;
        StringAssert.Contains(bearerDescription, "Explicit idtyp must be user");
        StringAssert.Contains(bearerDescription, "when idtyp is absent, scp is required");
        StringAssert.Contains(bearerDescription, "roles-only tokens are rejected");

        VerifyOperationContracts(operations);
        VerifyHeaders(operations);
        VerifyProblemDetails(root);
        VerifySchemas(root);
    }

    #endregion

    #region Private Methods

    private static string FindArtifact()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "artifacts", "openapi", "beacon-ar-v1.json")))
            directory = directory.Parent;

        Assert.IsNotNull(directory, "The Beacon AR example root could not be located from the test output directory.");
        string path = Path.Combine(directory.FullName, "artifacts", "openapi", "beacon-ar-v1.json");
        Assert.IsTrue(File.Exists(path), $"The generated OpenAPI artifact was not found at {path}.");
        return path;
    }

    private static List<JsonElement> GetOperations(JsonElement paths)
    {
        HashSet<string> methods = new(StringComparer.Ordinal) { "get", "post", "put", "delete", "patch" };
        return paths.EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .Where(property => methods.Contains(property.Name))
            .Select(property => property.Value)
            .ToList();
    }

    private static string OperationId(JsonElement operation) => operation.GetProperty("operationId").GetString()!;

    private static void VerifyOperationContracts(IReadOnlyList<JsonElement> operations)
    {
        foreach (JsonElement operation in operations)
        {
            string operationId = OperationId(operation);
            if (string.Equals(operationId, "getApiRoot", StringComparison.Ordinal))
            {
                Assert.AreEqual(0, operation.GetProperty("security").GetArrayLength());
                JsonProperty[] responses = operation.GetProperty("responses").EnumerateObject().ToArray();
                Assert.AreEqual(1, responses.Length);
                Assert.AreEqual("200", responses[0].Name);
                Assert.IsTrue(responses[0].Value.GetProperty("content").GetProperty("application/json").TryGetProperty("example", out _));
                continue;
            }

            Assert.IsTrue(operation.GetProperty("security").EnumerateArray().Any(requirement => requirement.TryGetProperty("bearerAuth", out _)),
                $"{operationId} must require bearerAuth.");

            bool hasIfMatch = Parameters(operation).Any(parameter => parameter.GetProperty("name").GetString() == "If-Match");
            Assert.AreEqual(IfMatchOperations.Contains(operationId), hasIfMatch, $"Unexpected If-Match contract for {operationId}.");
            if (hasIfMatch)
            {
                JsonElement parameter = Parameters(operation).Single(value => value.GetProperty("name").GetString() == "If-Match");
                Assert.IsTrue(parameter.GetProperty("required").GetBoolean(), $"{operationId} If-Match must be required.");
                StringAssert.Contains(parameter.GetProperty("description").GetString()!, "strong ETag");
            }

            VerifyProblemResponse(operation, "401", true);
            VerifyProblemResponse(operation, "403", true);
            VerifyProblemResponse(operation, "500", true);
            VerifyProblemResponse(operation, "400", BadRequestOperations.Contains(operationId));
            VerifyProblemResponse(operation, "404", NotFoundOperations.Contains(operationId));
            VerifyProblemResponse(operation, "409", ConflictOperations.Contains(operationId));
            VerifyProblemResponse(operation, "412", IfMatchOperations.Contains(operationId));
            VerifyProblemResponse(operation, "428", IfMatchOperations.Contains(operationId));
            VerifyProblemResponse(operation, "413", JsonBodyOperations.Contains(operationId));
            VerifyProblemResponse(operation, "415", JsonBodyOperations.Contains(operationId));

            bool hasRequestBody = operation.TryGetProperty("requestBody", out JsonElement requestBody);
            Assert.AreEqual(JsonBodyOperations.Contains(operationId), hasRequestBody, $"Unexpected request-body contract for {operationId}.");
            if (hasRequestBody)
            {
                JsonProperty[] mediaTypes = requestBody.GetProperty("content").EnumerateObject().ToArray();
                Assert.AreEqual(1, mediaTypes.Length, $"{operationId} must advertise exactly one request media type.");
                Assert.AreEqual("application/json", mediaTypes[0].Name, $"{operationId} accepts only application/json.");
                Assert.IsTrue(mediaTypes[0].Value.TryGetProperty("example", out _), $"{operationId} must provide a request example.");
            }

            foreach (JsonProperty responseProperty in operation.GetProperty("responses").EnumerateObject().Where(value => value.Name is "200" or "201"))
            {
                if (responseProperty.Value.TryGetProperty("content", out JsonElement content) && content.TryGetProperty("application/json", out JsonElement media))
                    Assert.IsTrue(media.TryGetProperty("example", out _), $"{operationId} {responseProperty.Name} must provide a response example.");
            }
        }

        string[] creationOperations = ["createProduct", "createCustomer", "createAddress", "createCarrier", "createQuote", "createSalesOrder"];
        foreach (string operationId in creationOperations)
        {
            JsonElement parameter = Parameters(operations.Single(value => OperationId(value) == operationId))
                .Single(value => value.GetProperty("name").GetString() == "Idempotency-Key");
            JsonElement schema = parameter.GetProperty("schema");
            Assert.AreEqual(128, schema.GetProperty("maxLength").GetInt32());
            Assert.AreEqual(1, schema.GetProperty("minLength").GetInt32());
            StringAssert.Contains(parameter.GetProperty("description").GetString()!, "exact retry");
        }
    }

    private static void VerifyProblemResponse(JsonElement operation, string statusCode, bool expected)
    {
        string operationId = OperationId(operation);
        bool present = operation.GetProperty("responses").TryGetProperty(statusCode, out JsonElement response);
        Assert.AreEqual(expected, present, $"Unexpected {statusCode} response contract for {operationId}.");
        if (!present)
            return;

        JsonElement media = response.GetProperty("content").GetProperty("application/problem+json");
        Assert.AreEqual("#/components/schemas/ApiProblemDetails", media.GetProperty("schema").GetProperty("$ref").GetString());
        Assert.IsTrue(media.TryGetProperty("example", out _), $"{operationId} {statusCode} must provide a Problem Details example.");
    }

    private static List<JsonElement> Parameters(JsonElement operation) =>
        operation.TryGetProperty("parameters", out JsonElement parameters) ? parameters.EnumerateArray().ToList() : [];

    private static void VerifyHeaders(IReadOnlyList<JsonElement> operations)
    {
        HashSet<string> etagOperations =
        [
            "getProduct", "createProduct", "updateProduct", "getCustomer", "createCustomer", "updateCustomer",
            "getAddress", "createAddress", "updateAddress", "getCarrier", "createCarrier", "updateCarrier",
            "getQuote", "createQuote", "updateQuote", "transitionQuoteStatus", "convertQuoteToSalesOrder",
            "getSalesOrder", "createSalesOrder", "updateSalesOrder", "transitionSalesOrderStatus",
        ];
        HashSet<string> locationOperations =
        [
            "createProduct", "createCustomer", "createAddress", "createCarrier", "createQuote", "createSalesOrder",
            "convertQuoteToSalesOrder",
        ];

        foreach (JsonElement operation in operations)
        {
            string operationId = OperationId(operation);
            foreach (JsonProperty response in operation.GetProperty("responses").EnumerateObject().Where(value => value.Name is "200" or "201"))
            {
                bool hasEtag = response.Value.TryGetProperty("headers", out JsonElement headers) && headers.TryGetProperty("ETag", out _);
                Assert.AreEqual(etagOperations.Contains(operationId), hasEtag, $"Unexpected ETag contract for {operationId} {response.Name}.");

                bool hasLocation = response.Value.TryGetProperty("headers", out headers) && headers.TryGetProperty("Location", out _);
                Assert.AreEqual(response.Name == "201" && locationOperations.Contains(operationId), hasLocation,
                    $"Unexpected Location contract for {operationId} {response.Name}.");
            }
        }
    }

    private static void VerifyProblemDetails(JsonElement root)
    {
        JsonElement schema = Schema(root, "ApiProblemDetails");
        HashSet<string> required = schema.GetProperty("required").EnumerateArray().Select(value => value.GetString()!).ToHashSet(StringComparer.Ordinal);
        CollectionAssert.IsSubsetOf(ProblemRequiredProperties, required.ToArray());
        JsonElement errors = schema.GetProperty("properties").GetProperty("errors");
        Assert.AreEqual("array", errors.GetProperty("additionalProperties").GetProperty("type").GetString());
        Assert.IsTrue(schema.TryGetProperty("example", out _));
    }

    private static void VerifySchemas(JsonElement root)
    {
        JsonElement product = Schema(root, "ProductCreateRequest");
        HashSet<string> required = product.GetProperty("required").EnumerateArray().Select(value => value.GetString()!).ToHashSet(StringComparer.Ordinal);
        Assert.IsTrue(required.Contains("sku"));
        Assert.IsFalse(required.Contains("thumbnailUrl"));
        Assert.IsTrue(product.GetProperty("properties").GetProperty("thumbnailUrl").GetProperty("nullable").GetBoolean());
        Assert.AreEqual(2, product.GetProperty("properties").GetProperty("sku").GetProperty("minLength").GetInt32());
        Assert.AreEqual(32, product.GetProperty("properties").GetProperty("sku").GetProperty("maxLength").GetInt32());
        Assert.AreEqual(0.0001m, product.GetProperty("properties").GetProperty("unitPrice").GetProperty("minimum").GetDecimal());

        JsonElement productResponse = Schema(root, "ProductView").GetProperty("properties");
        Assert.IsTrue(productResponse.GetProperty("modifiedByUserId").GetProperty("nullable").GetBoolean());
        Assert.AreEqual("integer", productResponse.GetProperty("modifiedByUserId").GetProperty("type").GetString());
        Assert.IsTrue(productResponse.GetProperty("modificationDate").GetProperty("nullable").GetBoolean());
        Assert.AreEqual("date-time", productResponse.GetProperty("modificationDate").GetProperty("format").GetString());
        Assert.IsTrue(productResponse.TryGetProperty("createdByUserDisplayName", out _));
        Assert.IsTrue(productResponse.TryGetProperty("modifiedByUserDisplayName", out _));
        Assert.IsTrue(productResponse.TryGetProperty("version", out _));
        Assert.IsFalse(productResponse.TryGetProperty("rowVersion", out _));

        JsonElement addressResponse = Schema(root, "CustomerAddressView").GetProperty("properties");
        Assert.IsTrue(addressResponse.TryGetProperty("customerAccountNumber", out _));
        Assert.IsTrue(addressResponse.TryGetProperty("customerName", out _));
        Assert.IsTrue(addressResponse.TryGetProperty("addressTypeDisplayName", out _));
        Assert.IsTrue(addressResponse.TryGetProperty("type", out _));
        Assert.IsTrue(addressResponse.TryGetProperty("defaultBilling", out _));
        Assert.IsTrue(addressResponse.TryGetProperty("defaultShipping", out _));
        Assert.IsFalse(addressResponse.TryGetProperty("addressTypeCode", out _));

        JsonElement rootResponse = Schema(root, "ApiRootResponse");
        HashSet<string> rootProperties = rootResponse.GetProperty("properties").EnumerateObject().Select(static property => property.Name).ToHashSet(StringComparer.Ordinal);
        CollectionAssert.AreEquivalent(RootResponseProperties, rootProperties.ToArray());
        Assert.IsTrue(rootResponse.TryGetProperty("example", out _));

        JsonElement customer = Schema(root, "CustomerCreateRequest");
        Assert.AreEqual(5, customer.GetProperty("properties").GetProperty("paymentTermsDays").GetProperty("enum").GetArrayLength());
        Assert.IsTrue(customer.GetProperty("properties").GetProperty("phone").GetProperty("nullable").GetBoolean());

        JsonElement quote = Schema(root, "QuoteCreateRequest");
        Assert.AreEqual("date", quote.GetProperty("properties").GetProperty("quoteDate").GetProperty("format").GetString());
        Assert.AreEqual(1, quote.GetProperty("properties").GetProperty("lines").GetProperty("minItems").GetInt32());
        Assert.AreEqual(1000, quote.GetProperty("properties").GetProperty("notes").GetProperty("maxLength").GetInt32());

        JsonElement order = Schema(root, "SalesOrderCreateRequest");
        Assert.IsTrue(order.GetProperty("properties").GetProperty("requestedShipDate").GetProperty("nullable").GetBoolean());
        Assert.AreEqual("date", order.GetProperty("properties").GetProperty("requestedShipDate").GetProperty("format").GetString());

        JsonElement line = Schema(root, "SalesLineRequest").GetProperty("properties");
        Assert.AreEqual(1, line.GetProperty("quantity").GetProperty("minimum").GetInt32());
        Assert.AreEqual(100m, line.GetProperty("discountPercent").GetProperty("maximum").GetDecimal());

        foreach (JsonProperty component in root.GetProperty("components").GetProperty("schemas").EnumerateObject())
        {
            if (!component.Value.TryGetProperty("properties", out JsonElement properties))
                continue;
            foreach (JsonProperty property in properties.EnumerateObject().Where(value => IsDecimalProperty(value.Name)))
                Assert.AreEqual("decimal", property.Value.GetProperty("format").GetString(), $"{component.Name}.{property.Name} must use the decimal format.");
        }

        int schemaExamples = root.GetProperty("components").GetProperty("schemas").EnumerateObject().Count(value => value.Value.TryGetProperty("example", out _));
        Assert.IsGreaterThanOrEqualTo(25, schemaExamples, "The client contract must include representative component examples.");
    }

    private static bool IsDecimalProperty(string name) => name is
        "unitPrice" or "creditLimit" or "discountPercent" or "subtotal" or "discountTotal" or "grandTotal" or
        "lineSubtotal" or "discountAmount" or "lineTotal";

    private static JsonElement Schema(JsonElement root, string name) =>
        root.GetProperty("components").GetProperty("schemas").GetProperty(name);

    #endregion
}
