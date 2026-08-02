using BeaconAr.Data.Receivables.Context;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Operations;

public sealed class AuditLogRepository : RepositoryBase<ReceivablesDbContext, int>, IAuditLogRepository
{
    #region Constructors

    public AuditLogRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public void Add(AuditLog auditLog) => EntityContext.AuditLogs.Add(auditLog);

    #endregion
}
