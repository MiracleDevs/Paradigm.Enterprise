using System.Text.Json.Nodes;
using System.Globalization;
using BeaconAr.WebApi.Endpoints;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BeaconAr.WebApi.OpenApi;

public sealed class BeaconArDocumentFilter : IDocumentFilter
{
    #region Fields

    private static readonly IReadOnlyDictionary<string, string> ProblemResponses = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["400"] = "The request is malformed or violates validation rules.",
        ["401"] = "A valid bearer access token is required.",
        ["403"] = "The caller does not have the required permission.",
        ["404"] = "The requested resource was not found.",
        ["409"] = "The request conflicts with current resource or idempotency state.",
        ["412"] = "The supplied entity tag is stale.",
        ["413"] = "The request body exceeds the configured size limit.",
        ["415"] = "The request body does not use application/json.",
        ["428"] = "An If-Match precondition is required.",
        ["500"] = "An unexpected server error occurred.",
    };

    private static readonly Dictionary<string, string[]> SearchParameterOrder =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["searchProducts"] = ["search", "pageNumber", "pageSize", "sortField", "sortDirection", "active"],
            ["searchCustomers"] = ["search", "pageNumber", "pageSize", "sortField", "sortDirection", "active"],
            ["searchCarriers"] = ["search", "pageNumber", "pageSize", "sortField", "sortDirection", "active"],
            ["searchAddresses"] = ["search", "pageNumber", "pageSize", "sortField", "sortDirection", "customerId", "type", "usage"],
            ["searchQuotes"] = ["search", "status", "customerId", "pageNumber", "pageSize", "sortField", "sortDirection"],
            ["searchSalesOrders"] = ["search", "status", "customerId", "sourceQuoteId", "pageNumber", "pageSize", "sortField", "sortDirection"],
        };

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

    private static readonly HashSet<string> ETagOperations = new(StringComparer.Ordinal)
    {
        "getProduct", "createProduct", "updateProduct",
        "getCustomer", "createCustomer", "updateCustomer",
        "getAddress", "createAddress", "updateAddress",
        "getCarrier", "createCarrier", "updateCarrier",
        "getQuote", "createQuote", "updateQuote", "transitionQuoteStatus", "convertQuoteToSalesOrder",
        "getSalesOrder", "createSalesOrder", "updateSalesOrder", "transitionSalesOrderStatus",
    };

    private static readonly HashSet<string> LocationOperations = new(StringComparer.Ordinal)
    {
        "createProduct", "createCustomer", "createAddress", "createCarrier", "createQuote", "createSalesOrder",
        "convertQuoteToSalesOrder",
    };

    private static readonly HashSet<string> IdempotentCreateOperations = new(StringComparer.Ordinal)
    {
        "createProduct", "createCustomer", "createAddress", "createCarrier", "createQuote", "createSalesOrder",
    };

    #endregion

    #region Public Methods

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Info.Title = ApiMetadata.Name;
        swaggerDoc.Info.Version = ApiMetadata.OpenApiVersion;
        swaggerDoc.Info.Description = "Delegated Microsoft Entra user access tokens (v2) only. Explicit idtyp must be user; without idtyp, delegated scp is required and roles-only client-credentials tokens are rejected. business.write implies business.read.";
        swaggerDoc.Components ??= new OpenApiComponents();
        swaggerDoc.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
        swaggerDoc.Components.SecuritySchemes["bearerAuth"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Delegated Microsoft Entra v2 user access token issued for the Beacon AR API audience. oid and sub are required. Explicit idtyp must be user; when idtyp is absent, scp is required and roles-only tokens are rejected.",
        };

        AddProblemSchema(swaggerDoc);
        ApplySchemaContract(swaggerDoc);

        OpenApiSecuritySchemeReference reference = new("bearerAuth", swaggerDoc);
        foreach (OpenApiPathItem path in swaggerDoc.Paths.Values)
        {
            if (path.Operations is null)
                continue;

            foreach (OpenApiOperation operation in path.Operations.Values)
                ApplyOperationContract(swaggerDoc, operation, reference);
        }

    }

    #endregion

    #region Private Methods

    private static void AddProblemSchema(OpenApiDocument document)
    {
        document.Components!.Schemas ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        document.Components.Schemas["ApiProblemDetails"] = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Required = new HashSet<string>(["status", "code", "title", "detail", "correlationId"], StringComparer.Ordinal),
            Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                ["status"] = IntegerSchema(400, 100, 599),
                ["code"] = StringSchema(1, 100, "Stable machine-readable error code."),
                ["title"] = StringSchema(1, 200, "Short, safe error summary."),
                ["detail"] = StringSchema(1, 1000, "Safe user-facing explanation without internal exception details."),
                ["errors"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Description = "Validation messages keyed by camelCase request-property path.",
                    AdditionalProperties = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema { Type = JsonSchemaType.String },
                    },
                },
                ["correlationId"] = StringSchema(1, 200, "W3C trace/correlation identifier for support diagnostics."),
            },
            Example = ParseExample("""
                {"status":400,"code":"validation_failed","title":"One or more validation errors occurred.","detail":"The request could not be processed.","errors":{"lines.0.quantity":["Quantity must be greater than zero."]},"correlationId":"00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"}
                """),
        };
    }

    private static void ApplyOperationContract(OpenApiDocument document, OpenApiOperation operation, OpenApiSecuritySchemeReference reference)
    {
        if (string.Equals(operation.OperationId, "getApiRoot", StringComparison.Ordinal))
        {
            operation.Security = [];
            AddSuccessMetadata(operation);
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement { [reference] = [] });
        operation.Responses ??= new OpenApiResponses();
        string operationId = operation.OperationId ?? string.Empty;
        NormalizeQueryParameters(operation, operationId);

        bool requiresIfMatch = false;
        foreach (IOpenApiParameter parameter in operation.Parameters ?? [])
        {
            if (parameter is not OpenApiParameter mutable)
                continue;

            ApplyParameterContract(mutable);
            if (mutable.In == ParameterLocation.Header && string.Equals(mutable.Name, "If-Match", StringComparison.OrdinalIgnoreCase))
            {
                mutable.Required = true;
                mutable.Description = "Required strong ETag from the latest resource response. Wildcards, weak tags, and multiple tags are rejected.";
                requiresIfMatch = true;
            }
        }

        bool hasJsonBody = false;
        if (operation.RequestBody is OpenApiRequestBody body)
        {
            body.Content?.Remove("application/*+json");
            if (body.Content?.TryGetValue("application/json", out OpenApiMediaType? mediaType) == true)
            {
                mediaType.Example = RequestExample(operation.OperationId);
                hasJsonBody = true;
            }
        }

        foreach (string statusCode in ProblemResponses.Keys)
            operation.Responses.Remove(statusCode);

        AddProblemResponse(document, operation, "401");
        AddProblemResponse(document, operation, "403");
        AddProblemResponse(document, operation, "500");
        if (BadRequestOperations.Contains(operationId))
            AddProblemResponse(document, operation, "400");
        if (NotFoundOperations.Contains(operationId))
            AddProblemResponse(document, operation, "404");
        if (ConflictOperations.Contains(operationId))
            AddProblemResponse(document, operation, "409");
        if (hasJsonBody)
        {
            AddProblemResponse(document, operation, "413");
            AddProblemResponse(document, operation, "415");
        }
        if (requiresIfMatch)
        {
            AddProblemResponse(document, operation, "412");
            AddProblemResponse(document, operation, "428");
        }

        AddSuccessMetadata(operation);
    }

    private static void AddProblemResponse(OpenApiDocument document, OpenApiOperation operation, string statusCode) =>
        operation.Responses!.Add(statusCode, ProblemResponse(document, statusCode, ProblemResponses[statusCode]));

    private static void ApplyParameterContract(OpenApiParameter parameter)
    {
        if (parameter.Schema is not OpenApiSchema schema)
            return;

        switch (parameter.Name)
        {
            case "Idempotency-Key":
                schema.MinLength = 1;
                schema.MaxLength = 128;
                schema.Pattern = "^[\\x21-\\x7E]{1,128}$";
                parameter.Description = "Optional opaque key of 1-128 visible ASCII characters. Reuse only for an exact retry by the same authenticated user and creation operation; a changed payload returns 409.";
                break;
            case "search":
                schema.MaxLength = 320;
                parameter.Description = "Trimmed, case-insensitive free-text search.";
                break;
            case "pageNumber":
                SetIntegerBounds(schema, 1, null);
                schema.Default = JsonValue.Create(1);
                parameter.Description = "One-based page number. Defaults to 1.";
                break;
            case "pageSize":
                SetIntegerBounds(schema, 1, 100);
                schema.Default = JsonValue.Create(10);
                parameter.Description = "Number of results per page. Maximum 100.";
                break;
            case "id" or "customerId" or "sourceQuoteId":
                SetIntegerBounds(schema, 1, null);
                break;
            case "type":
                schema.MaxLength = 32;
                break;
        }
    }

    private static void NormalizeQueryParameters(OpenApiOperation operation, string operationId)
    {
        if (!SearchParameterOrder.TryGetValue(operationId, out string[]? order) || operation.Parameters is null)
            return;

        foreach (IOpenApiParameter parameter in operation.Parameters)
        {
            if (parameter is OpenApiParameter { In: ParameterLocation.Query } mutable &&
                !string.IsNullOrEmpty(mutable.Name))
            {
                mutable.Name = char.ToLowerInvariant(mutable.Name[0]) + mutable.Name[1..];
            }
        }

        Dictionary<string, int> positions = order.Select((name, index) => (name, index))
            .ToDictionary(item => item.name, item => item.index, StringComparer.Ordinal);
        operation.Parameters = operation.Parameters
            .OrderBy(parameter => positions.TryGetValue(parameter.Name ?? string.Empty, out int index) ? index : int.MaxValue)
            .ToList();
    }

    private static void AddSuccessMetadata(OpenApiOperation operation)
    {
        string operationId = operation.OperationId ?? string.Empty;
        if (ETagOperations.Contains(operationId))
        {
            foreach (string statusCode in operationId == "convertQuoteToSalesOrder" ? new[] { "200", "201" } : SuccessStatusCodes(operation))
                AddHeader(operation, statusCode, "ETag", "Strong entity tag representing the returned mutable resource version.", "\"AQIDBAUGBwg=\"");
        }

        if (LocationOperations.Contains(operationId))
        {
            AddHeader(operation, "201", "Location", "Canonical URL of the created resource.", LocationExample(operationId), "uri-reference");
        }

        if (IdempotentCreateOperations.Contains(operationId))
            AddHeader(operation, "201", "Idempotency-Replayed", "Present with value true when this response replays an earlier completed creation.", "true");

        JsonNode? example = ResponseExample(operationId);
        if (example is null)
            return;

        foreach (string statusCode in SuccessStatusCodes(operation))
        {
            if (operation.Responses!.TryGetValue(statusCode, out IOpenApiResponse? response) && response is OpenApiResponse mutable &&
                mutable.Content?.TryGetValue("application/json", out OpenApiMediaType? mediaType) == true)
                mediaType.Example = example.DeepClone();
        }
    }

    private static void AddHeader(OpenApiOperation operation, string statusCode, string name, string description, string example, string? format = null)
    {
        if (!operation.Responses!.TryGetValue(statusCode, out IOpenApiResponse? response) || response is not OpenApiResponse mutable)
            return;

        mutable.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.Ordinal);
        mutable.Headers[name] = new OpenApiHeader
        {
            Description = description,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = format },
            Example = JsonValue.Create(example),
        };
    }

    private static IEnumerable<string> SuccessStatusCodes(OpenApiOperation operation) =>
        operation.Responses!.Keys.Where(status => status is "200" or "201");

    private static OpenApiResponse ProblemResponse(OpenApiDocument document, string statusCode, string description) => new()
    {
        Description = description,
        Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal)
        {
            ["application/problem+json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchemaReference("ApiProblemDetails", document, null),
                Example = ProblemExample(statusCode),
            },
        },
    };

    private static OpenApiSchema IntegerSchema(int example, decimal? minimum, decimal? maximum) => new()
    {
        Type = JsonSchemaType.Integer,
        Format = "int32",
        Minimum = Numeric(minimum),
        Maximum = Numeric(maximum),
        Example = JsonValue.Create(example),
    };

    private static OpenApiSchema StringSchema(int minimumLength, int maximumLength, string description) => new()
    {
        Type = JsonSchemaType.String,
        MinLength = minimumLength,
        MaxLength = maximumLength,
        Description = description,
    };

    private static JsonNode ParseExample(string json) => JsonNode.Parse(json) ?? throw new InvalidOperationException("The OpenAPI example is invalid.");

    private static JsonNode ProblemExample(string statusCode)
    {
        (string code, string title) = statusCode switch
        {
            "400" => ("validation_failed", "One or more validation errors occurred."),
            "401" => ("unauthorized", "Authentication is required."),
            "403" => ("forbidden", "Access is forbidden."),
            "404" => ("not_found", "The resource was not found."),
            "409" => ("request_conflict", "The request conflicts with current state."),
            "412" => ("concurrency_conflict", "The resource has changed."),
            "413" => ("request_too_large", "Request too large"),
            "415" => ("unsupported_media_type", "Unsupported media type"),
            "428" => ("precondition_required", "A precondition is required."),
            "500" => ("internal_error", "An unexpected error occurred."),
            _ => throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "The problem response status is not supported."),
        };
        return ParseExample($$"""{"status":{{statusCode}},"code":"{{code}}","title":"{{title}}","detail":"The request could not be processed.","correlationId":"00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"}""");
    }

    private static string LocationExample(string operationId) => operationId switch
    {
        "createProduct" => "/api/v1/products/42",
        "createCustomer" => "/api/v1/customers/42",
        "createAddress" => "/api/v1/addresses/42",
        "createCarrier" => "/api/v1/carriers/42",
        "createQuote" => "/api/v1/quotes/42",
        _ => "/api/v1/sales-orders/42",
    };

    private static JsonNode? RequestExample(string? operationId) => operationId switch
    {
        "createProduct" or "updateProduct" => ParseExample("""{"sku":"BEACON-01","name":"Warehouse Beacon","category":"Hardware","unitPrice":49.9500,"stockQuantity":25,"thumbnailUrl":"https://cdn.example.test/beacon.png","isActive":true}"""),
        "createCustomer" or "updateCustomer" => ParseExample("""{"accountNumber":"ACME-001","name":"Acme Distribution","email":"ar@acme.example","phone":"+1-555-0100","creditLimit":25000.00,"paymentTermsDays":30,"isActive":true}"""),
        "createAddress" or "updateAddress" => ParseExample("""{"customerId":42,"type":"shipping","label":"Main warehouse","line1":"100 Beacon Way","line2":null,"city":"Seattle","state":"WA","postalCode":"98101","country":"US","defaultBilling":false,"defaultShipping":true}"""),
        "createCarrier" or "updateCarrier" => ParseExample("""{"code":"UPS","name":"United Parcel Service","serviceLevel":"Ground","trackingUrlTemplate":"https://example.test/track/{trackingNumber}","isActive":true}"""),
        "createQuote" or "updateQuote" => ParseExample("""{"customerId":42,"shippingAddressId":84,"quoteDate":"2026-08-01","validUntil":"2026-08-31","notes":"Valid for 30 days.","lines":[{"productId":7,"quantity":2,"unitPrice":49.9500,"discountPercent":5.00}]}"""),
        "transitionQuoteStatus" => ParseExample("""{"status":"sent"}"""),
        "createSalesOrder" or "updateSalesOrder" => ParseExample("""{"customerId":42,"shippingAddressId":84,"requestedShipDate":"2026-08-15","carrierId":3,"trackingNumber":"1Z999AA10123456784","lines":[{"productId":7,"quantity":2,"unitPrice":49.9500,"discountPercent":5.00}]}"""),
        "transitionSalesOrderStatus" => ParseExample("""{"status":"shipped","carrierId":3,"trackingNumber":"1Z999AA10123456784"}"""),
        _ => null,
    };

    private static JsonNode? ResponseExample(string operationId)
    {
        if (operationId.StartsWith("search", StringComparison.Ordinal))
            return ParseExample("""{"items":[],"pageNumber":1,"pageSize":10,"totalPages":0,"itemsCount":0}""");

        return operationId switch
        {
            "getApiRoot" => ParseExample("""{"name":"Beacon AR API","version":"1.0.0"}"""),
            "getCurrentUser" => ParseExample("""{"id":42,"displayName":"Alex Morgan","email":"alex@example.test","policies":["business.read","business.write"]}"""),
            "getDashboardSummary" => ParseExample("""{"products":120,"customers":48,"carriers":6,"openQuotes":12,"activeOrders":23,"asOf":"2026-08-01T15:30:00Z"}"""),
            "getProduct" or "createProduct" or "updateProduct" => ParseExample("""{"id":7,"sku":"BEACON-01","name":"Warehouse Beacon","category":"Hardware","unitPrice":49.9500,"stockQuantity":25,"thumbnailUrl":"https://cdn.example.test/beacon.png","isActive":true,"createdByUserId":42,"createdByUserDisplayName":"Alex Morgan","creationDate":"2026-08-01T15:30:00Z","modifiedByUserId":null,"modifiedByUserDisplayName":null,"modificationDate":null,"version":"AQIDBAUGBwg="}"""),
            "getCustomer" or "createCustomer" or "updateCustomer" => ParseExample("""{"id":42,"accountNumber":"ACME-001","name":"Acme Distribution","email":"ar@acme.example","phone":"+1-555-0100","creditLimit":25000.00,"paymentTermsDays":30,"isActive":true,"createdByUserId":42,"createdByUserDisplayName":"Alex Morgan","creationDate":"2026-08-01T15:30:00Z","modifiedByUserId":null,"modifiedByUserDisplayName":null,"modificationDate":null,"version":"AQIDBAUGBwg="}"""),
            "getAddress" or "createAddress" or "updateAddress" => ParseExample("""{"id":84,"customerId":42,"customerAccountNumber":"ACME-001","customerName":"Acme Distribution","addressTypeId":2,"type":"shipping","addressTypeDisplayName":"Shipping","label":"Main warehouse","line1":"100 Beacon Way","line2":null,"city":"Seattle","state":"WA","postalCode":"98101","country":"US","defaultBilling":false,"defaultShipping":true,"createdByUserId":42,"createdByUserDisplayName":"Alex Morgan","creationDate":"2026-08-01T15:30:00Z","modifiedByUserId":null,"modifiedByUserDisplayName":null,"modificationDate":null,"version":"AQIDBAUGBwg="}"""),
            "getCarrier" or "createCarrier" or "updateCarrier" => ParseExample("""{"id":3,"code":"UPS","name":"United Parcel Service","serviceLevel":"Ground","trackingUrlTemplate":"https://example.test/track/{trackingNumber}","isActive":true,"createdByUserId":42,"createdByUserDisplayName":"Alex Morgan","creationDate":"2026-08-01T15:30:00Z","modifiedByUserId":null,"modifiedByUserDisplayName":null,"modificationDate":null,"version":"AQIDBAUGBwg="}"""),
            "getQuote" or "createQuote" or "updateQuote" or "transitionQuoteStatus" => ParseExample("""{"id":91,"quoteNumber":"Q-2026-000091","customerId":42,"shippingAddressId":84,"quoteDate":"2026-08-01","validUntil":"2026-08-31","status":"sent","notes":"Valid for 30 days.","customerAccountNumber":"ACME-001","customerName":"Acme Distribution","customerEmail":"ar@acme.example","customerPhone":"+1-555-0100","shippingLabel":"Main warehouse","shippingLine1":"100 Beacon Way","shippingLine2":null,"shippingCity":"Seattle","shippingState":"WA","shippingPostalCode":"98101","shippingCountry":"US","shippingAddressTypeCode":"shipping","lines":[{"id":1,"productId":7,"sku":"BEACON-01","productName":"Warehouse Beacon","quantity":2,"unitPrice":49.9500,"discountPercent":5.00,"lineSubtotal":99.90,"discountAmount":5.00,"lineTotal":94.90}],"subtotal":99.90,"discountTotal":5.00,"grandTotal":94.90,"salesOrderId":null,"createdByUserId":42,"creationDate":"2026-08-01T15:30:00Z","modifiedByUserId":42,"modificationDate":"2026-08-01T15:35:00Z","version":"AQIDBAUGBwg="}"""),
            "getSalesOrder" or "createSalesOrder" or "updateSalesOrder" or "transitionSalesOrderStatus" or "convertQuoteToSalesOrder" => ParseExample("""{"id":101,"orderNumber":"SO-2026-000101","sourceQuoteId":91,"customerId":42,"shippingAddressId":84,"status":"draft","requestedShipDate":"2026-08-15","carrierId":3,"carrierName":"United Parcel Service","trackingNumber":"1Z999AA10123456784","customerAccountNumber":"ACME-001","customerName":"Acme Distribution","customerEmail":"ar@acme.example","customerPhone":"+1-555-0100","shippingLabel":"Main warehouse","shippingLine1":"100 Beacon Way","shippingLine2":null,"shippingCity":"Seattle","shippingState":"WA","shippingPostalCode":"98101","shippingCountry":"US","shippingAddressTypeCode":"shipping","lines":[{"id":1,"productId":7,"sku":"BEACON-01","productName":"Warehouse Beacon","quantity":2,"unitPrice":49.9500,"discountPercent":5.00,"lineSubtotal":99.90,"discountAmount":5.00,"lineTotal":94.90}],"subtotal":99.90,"discountTotal":5.00,"grandTotal":94.90,"createdByUserId":42,"creationDate":"2026-08-01T15:30:00Z","modifiedByUserId":null,"modificationDate":null,"version":"AQIDBAUGBwg="}"""),
            _ => null,
        };
    }

    private static void SetIntegerBounds(OpenApiSchema schema, decimal? minimum, decimal? maximum)
    {
        schema.Type = PreserveNull(schema, JsonSchemaType.Integer);
        schema.Minimum = Numeric(minimum);
        schema.Maximum = Numeric(maximum);
    }

    private static void ApplySchemaContract(OpenApiDocument document)
    {
        if (document.Components?.Schemas is null)
            return;

        ConfigureTransportViewSchemas(document.Components.Schemas);
        ConfigureSharedProperties(document.Components.Schemas);
        ConfigureRequestSchemas(document.Components.Schemas);
        ConfigureSchemaExamples(document.Components.Schemas);
    }

    private static void ConfigureTransportViewSchemas(IDictionary<string, IOpenApiSchema> schemas)
    {
        foreach (string schemaName in new[] { "ProductView", "CustomerView", "CustomerAddressView", "CarrierView" })
            RenameProperty(schemas, schemaName, "rowVersion", "version");

        RenameProperty(schemas, "CustomerAddressView", "addressTypeCode", "type");
        RenameProperty(schemas, "CustomerAddressView", "isDefaultBilling", "defaultBilling");
        RenameProperty(schemas, "CustomerAddressView", "isDefaultShipping", "defaultShipping");
    }

    private static void RenameProperty(
        IDictionary<string, IOpenApiSchema> schemas,
        string schemaName,
        string currentName,
        string transportName)
    {
        if (!schemas.TryGetValue(schemaName, out IOpenApiSchema? schema) ||
            schema is not OpenApiSchema mutable ||
            mutable.Properties is null ||
            !mutable.Properties.Remove(currentName, out IOpenApiSchema? property))
            return;

        mutable.Properties[transportName] = property;
        if (mutable.Required?.Remove(currentName) == true)
            mutable.Required.Add(transportName);
    }

    private static void ConfigureRequestSchemas(IDictionary<string, IOpenApiSchema> schemas)
    {
        SetRequired(schemas, "ProductCreateRequest", "sku", "name", "category", "unitPrice", "stockQuantity");
        SetRequired(schemas, "ProductUpdateRequest", "sku", "name", "category", "unitPrice", "stockQuantity", "isActive");
        SetRequired(schemas, "CustomerCreateRequest", "accountNumber", "name", "email", "creditLimit", "paymentTermsDays");
        SetRequired(schemas, "CustomerUpdateRequest", "accountNumber", "name", "email", "creditLimit", "paymentTermsDays", "isActive");
        SetRequired(schemas, "AddressCreateRequest", "customerId", "type", "label", "line1", "city", "postalCode", "country", "defaultBilling", "defaultShipping");
        SetRequired(schemas, "AddressUpdateRequest", "customerId", "type", "label", "line1", "city", "postalCode", "country", "defaultBilling", "defaultShipping");
        SetRequired(schemas, "CarrierCreateRequest", "code", "name", "serviceLevel");
        SetRequired(schemas, "CarrierUpdateRequest", "code", "name", "serviceLevel", "isActive");
        SetRequired(schemas, "QuoteCreateRequest", "customerId", "shippingAddressId", "quoteDate", "validUntil", "lines");
        SetRequired(schemas, "QuoteUpdateRequest", "customerId", "shippingAddressId", "quoteDate", "validUntil", "lines");
        SetRequired(schemas, "SalesOrderCreateRequest", "customerId", "shippingAddressId", "lines");
        SetRequired(schemas, "SalesOrderUpdateRequest", "customerId", "shippingAddressId", "lines");
        SetRequired(schemas, "QuoteStatusTransitionRequest", "status");
        SetRequired(schemas, "SalesOrderStatusTransitionRequest", "status");

        SetNullable(schemas, "ProductCreateRequest", "thumbnailUrl");
        SetNullable(schemas, "ProductUpdateRequest", "thumbnailUrl");
        SetNullable(schemas, "CustomerCreateRequest", "phone");
        SetNullable(schemas, "CustomerUpdateRequest", "phone");
        SetNullable(schemas, "AddressCreateRequest", "line2", "state");
        SetNullable(schemas, "AddressUpdateRequest", "line2", "state");
        SetNullable(schemas, "CarrierCreateRequest", "trackingUrlTemplate");
        SetNullable(schemas, "CarrierUpdateRequest", "trackingUrlTemplate");
        SetNullable(schemas, "QuoteCreateRequest", "notes");
        SetNullable(schemas, "QuoteUpdateRequest", "notes");
        SetNullable(schemas, "SalesOrderCreateRequest", "requestedShipDate", "carrierId", "trackingNumber");
        SetNullable(schemas, "SalesOrderUpdateRequest", "requestedShipDate", "carrierId", "trackingNumber");
        SetNullable(schemas, "SalesOrderStatusTransitionRequest", "carrierId", "trackingNumber");

        SetNullable(schemas, "CurrentUserDto", "email");
        SetNullable(schemas, "ProductView", "thumbnailUrl", "createdByUserId", "createdByUserDisplayName", "modifiedByUserId", "modifiedByUserDisplayName", "modificationDate");
        SetNullable(schemas, "CustomerView", "phone", "createdByUserId", "createdByUserDisplayName", "modifiedByUserId", "modifiedByUserDisplayName", "modificationDate");
        SetNullable(schemas, "CustomerAddressView", "line2", "state", "createdByUserId", "createdByUserDisplayName", "modifiedByUserId", "modifiedByUserDisplayName", "modificationDate");
        SetNullable(schemas, "CarrierView", "trackingUrlTemplate", "createdByUserId", "createdByUserDisplayName", "modifiedByUserId", "modifiedByUserDisplayName", "modificationDate");
        SetNullable(schemas, "QuoteDto", "notes", "customerPhone", "shippingLine2", "shippingState", "salesOrderId", "createdByUserId", "modifiedByUserId", "modificationDate");
        SetNullable(schemas, "QuoteView", "shippingAddressLine2", "shippingAddressState", "notes", "customerPhoneSnapshot", "shippingLine2Snapshot", "shippingStateSnapshot", "subtotal", "discountTotal", "grandTotal", "salesOrderId", "createdByUserId", "createdByUserDisplayName", "modifiedByUserId", "modifiedByUserDisplayName", "modificationDate", "deletionDate", "deletedByUserId", "deletedByUserDisplayName");
        SetNullable(schemas, "SalesOrderDto", "sourceQuoteId", "requestedShipDate", "carrierId", "carrierName", "trackingNumber", "customerPhone", "shippingLine2", "shippingState", "createdByUserId", "modifiedByUserId", "modificationDate");
        SetNullable(schemas, "SalesOrderView", "sourceQuoteId", "sourceQuoteNumber", "shippingAddressLine2", "shippingAddressState", "requestedShipDate", "carrierId", "carrierCode", "carrierName", "carrierServiceLevel", "trackingNumber", "customerAccountNumberSnapshot", "customerNameSnapshot", "customerEmailSnapshot", "customerPhoneSnapshot", "shippingLabelSnapshot", "shippingLine1Snapshot", "shippingLine2Snapshot", "shippingCitySnapshot", "shippingStateSnapshot", "shippingPostalCodeSnapshot", "shippingCountrySnapshot", "shippingAddressTypeCodeSnapshot", "subtotal", "discountTotal", "grandTotal", "createdByUserId", "createdByUserDisplayName", "modifiedByUserId", "modifiedByUserDisplayName", "modificationDate", "deletionDate", "deletedByUserId", "deletedByUserDisplayName");
    }

    private static void ConfigureSchemaExamples(IDictionary<string, IOpenApiSchema> schemas)
    {
        IReadOnlyDictionary<string, JsonNode> examples = new Dictionary<string, JsonNode>(StringComparer.Ordinal)
        {
            ["ApiRootResponse"] = ResponseExample("getApiRoot")!,
            ["ProductCreateRequest"] = RequestExample("createProduct")!,
            ["ProductUpdateRequest"] = RequestExample("updateProduct")!,
            ["CustomerCreateRequest"] = RequestExample("createCustomer")!,
            ["CustomerUpdateRequest"] = RequestExample("updateCustomer")!,
            ["AddressCreateRequest"] = RequestExample("createAddress")!,
            ["AddressUpdateRequest"] = RequestExample("updateAddress")!,
            ["CarrierCreateRequest"] = RequestExample("createCarrier")!,
            ["CarrierUpdateRequest"] = RequestExample("updateCarrier")!,
            ["QuoteCreateRequest"] = RequestExample("createQuote")!,
            ["QuoteUpdateRequest"] = RequestExample("updateQuote")!,
            ["QuoteStatusTransitionRequest"] = RequestExample("transitionQuoteStatus")!,
            ["SalesOrderCreateRequest"] = RequestExample("createSalesOrder")!,
            ["SalesOrderUpdateRequest"] = RequestExample("updateSalesOrder")!,
            ["SalesOrderStatusTransitionRequest"] = RequestExample("transitionSalesOrderStatus")!,
            ["SalesLineRequest"] = ParseExample("""{"productId":7,"quantity":2,"unitPrice":49.9500,"discountPercent":5.00}"""),
            ["CurrentUserDto"] = ResponseExample("getCurrentUser")!,
            ["DashboardSummaryDto"] = ResponseExample("getDashboardSummary")!,
            ["ProductView"] = ResponseExample("getProduct")!,
            ["CustomerView"] = ResponseExample("getCustomer")!,
            ["CustomerAddressView"] = ResponseExample("getAddress")!,
            ["CarrierView"] = ResponseExample("getCarrier")!,
            ["QuoteDto"] = ResponseExample("getQuote")!,
            ["SalesOrderDto"] = ResponseExample("getSalesOrder")!,
            ["SalesLineDto"] = ParseExample("""{"id":1,"productId":7,"sku":"BEACON-01","productName":"Warehouse Beacon","quantity":2,"unitPrice":49.9500,"discountPercent":5.00,"lineSubtotal":99.90,"discountAmount":5.00,"lineTotal":94.90}"""),
            ["QuoteView"] = ParseExample("""{"id":91,"quoteNumber":"Q-2026-000091","customerId":42,"customerAccountNumber":"ACME-001","customerName":"Acme Distribution","quoteDate":"2026-08-01","validUntil":"2026-08-31","statusId":2,"statusCode":"sent","statusDisplayName":"Sent","subtotal":99.90,"discountTotal":5.00,"grandTotal":94.90,"salesOrderId":null,"creationDate":"2026-08-01T15:30:00Z","rowVersion":"AQIDBAUGBwg="}"""),
            ["SalesOrderView"] = ParseExample("""{"id":101,"orderNumber":"SO-2026-000101","sourceQuoteId":91,"customerId":42,"customerAccountNumber":"ACME-001","customerName":"Acme Distribution","statusId":1,"statusCode":"draft","statusDisplayName":"Draft","requestedShipDate":"2026-08-15","carrierId":3,"carrierName":"United Parcel Service","trackingNumber":"1Z999AA10123456784","subtotal":99.90,"discountTotal":5.00,"grandTotal":94.90,"creationDate":"2026-08-01T15:30:00Z","rowVersion":"AQIDBAUGBwg="}"""),
        };

        foreach ((string name, JsonNode example) in examples)
        {
            if (schemas.TryGetValue(name, out IOpenApiSchema? schema) && schema is OpenApiSchema mutable)
                mutable.Example = example.DeepClone();
        }

        foreach ((string name, IOpenApiSchema schema) in schemas)
        {
            if (name.StartsWith("PageResult", StringComparison.Ordinal) && schema is OpenApiSchema mutable)
                mutable.Example = ResponseExample("searchProducts");
        }
    }

    private static void ConfigureSharedProperties(IDictionary<string, IOpenApiSchema> schemas)
    {
        foreach ((string name, IOpenApiSchema schema) in schemas)
        {
            if (schema is not OpenApiSchema mutable || mutable.Properties is null)
                continue;

            foreach ((string propertyName, IOpenApiSchema property) in mutable.Properties)
            {
                if (property is not OpenApiSchema value)
                    continue;

                ConfigureProperty(name, propertyName, value);
            }
        }
    }

    private static void ConfigureProperty(string schemaName, string propertyName, OpenApiSchema schema)
    {
        if (propertyName is "id" or "customerId" or "shippingAddressId" or "productId")
            SetIntegerBounds(schema, 1, null);
        if (propertyName is "pageNumber" or "pageSize")
            SetIntegerBounds(schema, propertyName == "pageNumber" ? 1 : 1, propertyName == "pageSize" ? 100 : null);

        switch (propertyName)
        {
            case "sku": SetStringBounds(schema, 2, 32); break;
            case "name" when schemaName.StartsWith("Product", StringComparison.Ordinal): SetStringBounds(schema, 2, 120); break;
            case "name": SetStringBounds(schema, 1, 120); break;
            case "category" or "label" or "city" or "state": SetStringBounds(schema, propertyName == "state" ? null : 1, 120); break;
            case "accountNumber" or "phone": SetStringBounds(schema, propertyName == "phone" ? null : 1, 50); break;
            case "email" or "customerEmail": SetStringBounds(schema, 1, 320, "email"); break;
            case "line1" or "line2": SetStringBounds(schema, propertyName == "line2" ? null : 1, 200); break;
            case "postalCode" or "code": SetStringBounds(schema, 1, 32); break;
            case "country": SetStringBounds(schema, 2, 2, null, "^[A-Z]{2}$"); break;
            case "type" when schemaName.Contains("Address", StringComparison.Ordinal):
                SetStringBounds(schema, 1, 8, null, "^(billing|shipping|both)$");
                break;
            case "serviceLevel": SetStringBounds(schema, 1, 120); break;
            case "thumbnailUrl": SetStringBounds(schema, null, 2048, "uri", "^https?://"); break;
            case "trackingUrlTemplate": SetStringBounds(schema, null, 2048, "uri", "^https://"); break;
            case "trackingNumber": SetStringBounds(schema, null, 200); break;
            case "notes": SetStringBounds(schema, null, 1000); break;
            case "lines": schema.MinItems = 1; break;
            case "stockQuantity": SetIntegerBounds(schema, 0, null); break;
            case "quantity": SetIntegerBounds(schema, 1, null); break;
            case "paymentTermsDays":
                schema.Type = JsonSchemaType.Integer;
                schema.Format = "int32";
                schema.Enum = [JsonValue.Create(0), JsonValue.Create(15), JsonValue.Create(30), JsonValue.Create(45), JsonValue.Create(60)];
                break;
            case "unitPrice":
                SetDecimal(schema, schemaName.StartsWith("Product", StringComparison.Ordinal) ? 0.0001m : 0m,
                    schemaName.StartsWith("Product", StringComparison.Ordinal) ? 999999999999999.9999m : 999999999999999.9999m, 0.0001m);
                break;
            case "creditLimit": SetDecimal(schema, 0m, 99999999999999999.99m, 0.01m); break;
            case "discountPercent": SetDecimal(schema, 0m, 100m, 0.01m); break;
            case "subtotal" or "discountTotal" or "grandTotal" or "lineSubtotal" or "discountAmount" or "lineTotal":
                SetDecimal(schema, 0m, 99999999999999999.99m, 0.01m);
                break;
        }
    }

    private static void SetRequired(IDictionary<string, IOpenApiSchema> schemas, string schemaName, params string[] properties)
    {
        if (schemas.TryGetValue(schemaName, out IOpenApiSchema? schema) && schema is OpenApiSchema mutable)
            mutable.Required = new HashSet<string>(properties, StringComparer.Ordinal);
    }

    private static void SetNullable(IDictionary<string, IOpenApiSchema> schemas, string schemaName, params string[] properties)
    {
        if (!schemas.TryGetValue(schemaName, out IOpenApiSchema? schema) || schema is not OpenApiSchema mutable || mutable.Properties is null)
            return;

        foreach (string propertyName in properties)
        {
            if (mutable.Properties.TryGetValue(propertyName, out IOpenApiSchema? property) && property is OpenApiSchema value)
                value.Type = InferType(value) | JsonSchemaType.Null;
        }
    }

    private static void SetStringBounds(OpenApiSchema schema, int? minimumLength, int? maximumLength, string? format = null, string? pattern = null)
    {
        schema.Type = PreserveNull(schema, JsonSchemaType.String);
        schema.MinLength = minimumLength;
        schema.MaxLength = maximumLength;
        schema.Format = format;
        schema.Pattern = pattern;
    }

    private static void SetDecimal(OpenApiSchema schema, decimal minimum, decimal maximum, decimal multipleOf)
    {
        schema.Type = PreserveNull(schema, JsonSchemaType.Number);
        schema.Format = "decimal";
        schema.Pattern = null;
        schema.Minimum = Numeric(minimum);
        schema.Maximum = Numeric(maximum);
        schema.MultipleOf = multipleOf;
    }

    private static string? Numeric(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static JsonSchemaType PreserveNull(OpenApiSchema schema, JsonSchemaType type) =>
        schema.Type is JsonSchemaType current && current.HasFlag(JsonSchemaType.Null) ? type | JsonSchemaType.Null : type;

    private static JsonSchemaType InferType(OpenApiSchema schema)
    {
        if (schema.Format is "int16" or "int32" or "int64")
            return JsonSchemaType.Integer;
        if (schema.Format is "float" or "double" or "decimal")
            return JsonSchemaType.Number;
        if (schema.Format is "date" or "date-time" or "email" or "uri" or "uri-reference")
            return JsonSchemaType.String;
        if (schema.Type is JsonSchemaType type)
        {
            JsonSchemaType nonNullType = type & ~JsonSchemaType.Null;
            if (nonNullType != 0)
                return nonNullType;
        }
        return JsonSchemaType.String;
    }

    #endregion
}
