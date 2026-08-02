using System.Text.Json.Serialization;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Reporting.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using Microsoft.AspNetCore.Mvc;
using BeaconAr.WebApi.Serialization;

namespace BeaconAr.WebApi;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    Converters = [
        typeof(CamelCaseStringEnumConverter<SortDirection>),
        typeof(CamelCaseStringEnumConverter<AddressUsage>),
        typeof(CamelCaseStringEnumConverter<BeaconAr.Domain.Sales.QuoteStatus>),
        typeof(CamelCaseStringEnumConverter<BeaconAr.Domain.Sales.SalesOrderStatus>),
        typeof(UtcDateTimeOffsetJsonConverter)],
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(CurrentUserDto))]
[JsonSerializable(typeof(DashboardSummaryDto))]
[JsonSerializable(typeof(ProductCreateRequest))]
[JsonSerializable(typeof(ProductUpdateRequest))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(ProductSearchRequest))]
[JsonSerializable(typeof(PageResult<ProductDto>))]
[JsonSerializable(typeof(CustomerCreateRequest))]
[JsonSerializable(typeof(CustomerUpdateRequest))]
[JsonSerializable(typeof(CustomerDto))]
[JsonSerializable(typeof(CustomerSearchRequest))]
[JsonSerializable(typeof(PageResult<CustomerDto>))]
[JsonSerializable(typeof(AddressCreateRequest))]
[JsonSerializable(typeof(AddressUpdateRequest))]
[JsonSerializable(typeof(AddressDto))]
[JsonSerializable(typeof(AddressSearchRequest))]
[JsonSerializable(typeof(PageResult<AddressDto>))]
[JsonSerializable(typeof(CarrierCreateRequest))]
[JsonSerializable(typeof(CarrierUpdateRequest))]
[JsonSerializable(typeof(CarrierDto))]
[JsonSerializable(typeof(CarrierSearchRequest))]
[JsonSerializable(typeof(PageResult<CarrierDto>))]
[JsonSerializable(typeof(QuoteCreateRequest))]
[JsonSerializable(typeof(QuoteUpdateRequest))]
[JsonSerializable(typeof(QuoteDto))]
[JsonSerializable(typeof(QuoteSummaryDto))]
[JsonSerializable(typeof(QuoteSearchRequest))]
[JsonSerializable(typeof(QuoteStatusTransitionRequest))]
[JsonSerializable(typeof(PageResult<QuoteSummaryDto>))]
[JsonSerializable(typeof(SalesOrderCreateRequest))]
[JsonSerializable(typeof(SalesOrderUpdateRequest))]
[JsonSerializable(typeof(SalesOrderDto))]
[JsonSerializable(typeof(SalesOrderSummaryDto))]
[JsonSerializable(typeof(SalesOrderSearchRequest))]
[JsonSerializable(typeof(SalesOrderStatusTransitionRequest))]
[JsonSerializable(typeof(PageResult<SalesOrderSummaryDto>))]
[JsonSerializable(typeof(SalesLineRequest))]
[JsonSerializable(typeof(SalesLineDto))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
public sealed partial class BeaconArApiJsonContext : JsonSerializerContext;
