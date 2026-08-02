using System.Data.Common;
using BeaconAr.Data.Receivables.Context;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Receivables.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData;

public sealed class AddressRepository : RepositoryBase<ReceivablesDbContext, int>, IAddressRepository
{
    #region Constructors

    public AddressRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public Task<CustomerAddress?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.CustomerAddresses.SingleOrDefaultAsync(address => address.Id == id, cancellationToken);

    public Task<int?> GetActiveTypeIdAsync(string code, CancellationToken cancellationToken) =>
        EntityContext.AddressTypes.AsNoTracking()
            .Where(type => type.Code == code && type.IsActive)
            .Select(type => (int?)type.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken) =>
        await EntityContext.Quotes.AsNoTracking().AnyAsync(quote => quote.ShippingAddressId == id, cancellationToken) ||
        await EntityContext.SalesOrders.AsNoTracking().AnyAsync(order => order.ShippingAddressId == id, cancellationToken);

    public async Task LockCustomersAsync(IEnumerable<int> customerIds, CancellationToken cancellationToken)
    {
        int[] ids = customerIds.Distinct().OrderBy(id => id).ToArray();
        if (ids.Length == 0)
            return;

        DbConnection connection = GetDbConnection();
        await EntityContext.Database.OpenConnectionAsync(cancellationToken);
        foreach (int id in ids)
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT [Id] FROM [dbo].[Customer] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = @Id";
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = "@Id";
            parameter.Value = id;
            command.Parameters.Add(parameter);
            UnitOfWork.UseTransaction(command);
            await command.ExecuteScalarAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<CustomerAddress>> GetDefaultsForUpdateAsync(
        IEnumerable<int> customerIds, CancellationToken cancellationToken)
    {
        int[] ids = customerIds.Distinct().ToArray();
        return await EntityContext.CustomerAddresses
            .Where(address => ids.Contains(address.CustomerId) && (address.IsDefaultBilling || address.IsDefaultShipping))
            .OrderBy(address => address.CustomerId)
            .ThenBy(address => address.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task ReparentAsync(CustomerAddress address, AddressUpdateRequest request, int addressTypeId,
        int userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (address.CustomerId == request.CustomerId)
            return;

        DbConnection connection = GetDbConnection();
        await EntityContext.Database.OpenConnectionAsync(cancellationToken);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE [dbo].[CustomerAddress]
            SET [CustomerId] = @CustomerId, [AddressTypeId] = @AddressTypeId, [Label] = @Label,
                [Line1] = @Line1, [Line2] = @Line2, [City] = @City, [State] = @State,
                [PostalCode] = @PostalCode, [Country] = @Country,
                [IsDefaultBilling] = @DefaultBilling, [IsDefaultShipping] = @DefaultShipping,
                [ModifiedByUserId] = @ModifiedByUserId, [ModificationDate] = @ModificationDate
            OUTPUT INSERTED.[RowVersion]
            WHERE [Id] = @Id AND [RowVersion] = @RowVersion
            """;
        AddParameter(command, "@CustomerId", request.CustomerId);
        AddParameter(command, "@AddressTypeId", addressTypeId);
        AddParameter(command, "@Label", request.Label);
        AddParameter(command, "@Line1", request.Line1);
        AddParameter(command, "@Line2", request.Line2);
        AddParameter(command, "@City", request.City);
        AddParameter(command, "@State", request.State);
        AddParameter(command, "@PostalCode", request.PostalCode);
        AddParameter(command, "@Country", request.Country);
        AddParameter(command, "@DefaultBilling", request.DefaultBilling);
        AddParameter(command, "@DefaultShipping", request.DefaultShipping);
        AddParameter(command, "@ModifiedByUserId", userId);
        AddParameter(command, "@ModificationDate", now);
        AddParameter(command, "@Id", address.Id);
        DbParameter versionParameter = AddParameter(command, "@RowVersion", address.RowVersion);
        versionParameter.DbType = System.Data.DbType.Binary;
        UnitOfWork.UseTransaction(command);
        byte[]? rowVersion = await command.ExecuteScalarAsync(cancellationToken) as byte[];
        if (rowVersion is null)
            throw new DbUpdateConcurrencyException("The address was changed while it was being re-parented.");

        EntityContext.Entry(address).State = EntityState.Detached;
        address.Replace(request, addressTypeId, userId, now);
    }

    public void Add(CustomerAddress address) => EntityContext.CustomerAddresses.Add(address);

    public void Delete(CustomerAddress address) => EntityContext.CustomerAddresses.Remove(address);

    #endregion

    #region Private Methods

    private static DbParameter AddParameter(DbCommand command, string name, object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
        return parameter;
    }

    #endregion
}
