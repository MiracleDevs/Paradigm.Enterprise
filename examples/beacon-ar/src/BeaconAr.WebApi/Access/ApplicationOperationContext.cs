using BeaconAr.Domain.Operations;

namespace BeaconAr.WebApi.Access;

public sealed class ApplicationOperationContext : IApplicationOperationContext
{
    #region Fields

    private int? _userId;
    private string? _correlationId;

    #endregion

    #region Properties

    public int UserId => _userId ?? throw new InvalidOperationException("The request operation context has not been initialized.");

    public string CorrelationId => _correlationId ?? throw new InvalidOperationException("The request operation context has not been initialized.");

    #endregion

    #region Public Methods

    public void Initialize(int userId, string correlationId)
    {
        if (_userId.HasValue || userId <= 0 || string.IsNullOrWhiteSpace(correlationId))
            throw new InvalidOperationException("The request operation context can only be initialized once with valid values.");
        _userId = userId;
        _correlationId = correlationId;
    }

    #endregion
}
