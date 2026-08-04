using BeaconAr.Data.Operations.Context;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Operations.Entities;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Operations.Repositories;

public sealed class AuditLogRepository : RepositoryBase<OperationsDbContext, int>, IAuditLogRepository
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
