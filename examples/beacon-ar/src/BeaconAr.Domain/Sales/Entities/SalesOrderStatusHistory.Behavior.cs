namespace BeaconAr.Domain.Sales.Entities;

public partial class SalesOrderStatusHistory
{
    #region Public Methods

    public static SalesOrderStatusHistory Create(SalesOrder order, int actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(order);
        return new SalesOrderStatusHistory
        {
            SalesOrder = order,
            StatusId = order.StatusId,
            CreatedByUserId = actorId,
            CreationDate = now,
        };
    }

    #endregion
}
