using BeaconAr.Domain.Receivables.Generated;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Operations.Repositories;

public interface IAuditLogRepository : IRepository
{
    void Add(AuditLog auditLog);
}
