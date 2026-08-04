using OrderState = BeaconAr.Interfaces.Sales.Enums.SalesOrderStatus;
using Paradigm.Enterprise.Domain.Exceptions;

namespace BeaconAr.Domain.Sales.Entities;

public partial class SalesOrderStatusHistory
{
    #region Public Methods

    public static SalesOrderStatusHistory Create(SalesOrder order, int actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(order);
        var rules = new DomainValidator();
        rules.Assert(Enum.IsDefined((OrderState)order.StatusId), "Sales order history status is invalid.");
        rules.Assert(actorId > 0, "Sales order history actor ID must be positive.");
        rules.Assert(now != default, "Sales order history creation time is required.");
        rules.ThrowIfAny();
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
