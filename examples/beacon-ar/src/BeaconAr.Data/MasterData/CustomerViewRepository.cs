using BeaconAr.Data.Mappers;
using BeaconAr.Data.MasterData.StoredProcedures;
using BeaconAr.Data.Receivables.Context;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData;

public sealed class CustomerViewRepository : RepositoryBase<ReceivablesDbContext, int>, ICustomerViewRepository
{
    #region Fields

    private static readonly SearchCustomerProcedure SearchProcedure = new();

    #endregion

    #region Constructors

    public CustomerViewRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Static Constructors

    static CustomerViewRepository() => StoreProcedureMappersRegisterer.RegisterMappers();

    #endregion

    #region Public Methods

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Customer? customer = await EntityContext.Customers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return customer is null ? null : Map(customer);
    }

    public async Task<PageResult<CustomerDto>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken)
    {
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        string sort = string.IsNullOrWhiteSpace(request.SortField) ? "id" : request.SortField.Trim().ToLowerInvariant();
        cancellationToken.ThrowIfCancellationRequested();
        var (count, rows) = await SearchProcedure.ExecuteAsync(GetDbConnection(), new CustomerSearchParameters
        {
            Search = search,
            Active = request.Active,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortField = sort,
            SortDirection = request.SortDirection == SortDirection.Desc ? "desc" : "asc",
        }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return PageResultFactory.Create((rows ?? []).Select(Map).ToArray(), request.PageNumber, request.PageSize, count);
    }

    #endregion

    #region Private Methods

    private static CustomerDto Map(Customer customer) => new(
        customer.Id, customer.AccountNumber, customer.Name, customer.Email, customer.Phone, customer.CreditLimit,
        customer.PaymentTermsDays, customer.IsActive, customer.CreatedByUserId, customer.CreationDate,
        customer.ModifiedByUserId, customer.ModificationDate, VersionTokenCodec.Encode(customer.RowVersion));

    private static CustomerDto Map(CustomerSearchRow customer) => new(
        customer.Id, customer.AccountNumber, customer.Name, customer.Email, customer.Phone, customer.CreditLimit,
        customer.PaymentTermsDays, customer.IsActive, customer.CreatedByUserId, customer.CreationDate,
        customer.ModifiedByUserId, customer.ModificationDate, VersionTokenCodec.Encode(customer.RowVersion));

    #endregion
}
