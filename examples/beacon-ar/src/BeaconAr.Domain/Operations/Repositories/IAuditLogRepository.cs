using BeaconAr.Domain.Operations.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Operations.Repositories;

public interface IAuditLogRepository : IRepository
{
    void Add(AuditLog auditLog);
}
