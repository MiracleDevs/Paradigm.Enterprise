using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeaconAr.WebApi.Access;
using BeaconAr.Domain.Operations;
using BeaconAr.Providers.Operations;

namespace BeaconAr.WebApi.Http;

public sealed class CreationIdempotencyService
{
    #region Fields

    private readonly CurrentUserAccessor _currentUser;
    private readonly ICreationIdempotencyProvider _provider;
    private readonly TimeProvider _timeProvider;

    #endregion

    #region Constructors

    public CreationIdempotencyService(CurrentUserAccessor currentUser, ICreationIdempotencyProvider provider, TimeProvider timeProvider)
    {
        _currentUser = currentUser;
        _provider = provider;
        _timeProvider = timeProvider;
    }

    #endregion

    #region Public Methods

    public async Task<(T Value, bool Replayed)> ExecuteAsync<TRequest, T>(
        string operation,
        string? key,
        TRequest request,
        Func<Task<T>> create,
        Func<int, Task<T>> reload,
        Func<T, int> getId,
        CancellationToken cancellationToken)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            return (await create(), false);

        string normalized = key.Trim();
        if (normalized.Length > 128 || normalized.Any(static character => character is < '!' or > '~'))
            throw new ApiBoundaryException(400, "invalid_idempotency_key", "Idempotency-Key must contain 1-128 visible ASCII characters.");

        int userId = _currentUser.User?.Id ?? throw new InvalidOperationException("Current user is unavailable.");
        string keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(request, BeaconArApiJsonContext.Default.Options);
        string fingerprint = Convert.ToHexString(SHA256.HashData([.. Encoding.UTF8.GetBytes(operation), .. json]));
        _ = userId;
        CreationResult<T> result = await _provider.ExecuteAsync(
            new IdempotencyDescriptor(operation, Convert.FromHexString(keyHash), Convert.FromHexString(fingerprint), _timeProvider.GetUtcNow().AddHours(24)),
            operation["create-".Length..], create, reload, getId, cancellationToken);
        return (result.Value, !result.Created);
    }

    #endregion

}
