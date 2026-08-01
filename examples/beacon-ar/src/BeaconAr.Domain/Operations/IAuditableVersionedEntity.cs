using Paradigm.Enterprise.Interfaces;

namespace BeaconAr.Domain.Operations;

public interface IAuditableVersionedEntity : IAuditableEntity<DateTimeOffset, int>, IVersionedEntity
{
}
