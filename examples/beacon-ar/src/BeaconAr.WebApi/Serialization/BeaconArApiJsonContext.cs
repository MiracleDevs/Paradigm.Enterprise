using System.Text.Json.Serialization;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Reporting.Contracts;
using BeaconAr.Domain.Receivables.Entities;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.WebApi.Endpoints;
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
[JsonSerializable(typeof(ProductView))]
[JsonSerializable(typeof(ProductSearchRequest))]
[JsonSerializable(typeof(PageResult<ProductView>))]
[JsonSerializable(typeof(CustomerCreateRequest))]
[JsonSerializable(typeof(CustomerUpdateRequest))]
[JsonSerializable(typeof(CustomerView))]
[JsonSerializable(typeof(CustomerSearchRequest))]
[JsonSerializable(typeof(PageResult<CustomerView>))]
[JsonSerializable(typeof(AddressCreateRequest))]
[JsonSerializable(typeof(AddressUpdateRequest))]
[JsonSerializable(typeof(CustomerAddressView))]
[JsonSerializable(typeof(AddressSearchRequest))]
[JsonSerializable(typeof(PageResult<CustomerAddressView>))]
[JsonSerializable(typeof(CarrierCreateRequest))]
[JsonSerializable(typeof(CarrierUpdateRequest))]
[JsonSerializable(typeof(CarrierView))]
[JsonSerializable(typeof(CarrierSearchRequest))]
[JsonSerializable(typeof(PageResult<CarrierView>))]
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
[JsonSerializable(typeof(ApiRootResponse))]
public sealed partial class BeaconArApiJsonContext : JsonSerializerContext;
