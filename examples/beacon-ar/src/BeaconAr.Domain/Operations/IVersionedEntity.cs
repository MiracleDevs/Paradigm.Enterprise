namespace BeaconAr.Domain.Operations;

public interface IVersionedEntity
{
    #region Properties

    byte[] RowVersion { get; }

    #endregion
}
