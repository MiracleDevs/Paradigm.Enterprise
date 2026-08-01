using BeaconAr.Domain.MasterData.Application;

namespace BeaconAr.Domain.MasterData.Validation;

internal sealed class ValidationErrorBuilder
{
    #region Fields

    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    #endregion

    #region Public Methods

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out List<string>? messages))
        {
            messages = new List<string>();
            _errors.Add(field, messages);
        }

        messages.Add(message);
    }

    public void Assert(bool condition, string field, string message)
    {
        if (!condition)
            Add(field, message);
    }

    public void ThrowIfAny()
    {
        if (_errors.Count == 0)
            return;

        throw new MasterDataValidationException(
            _errors.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.AsReadOnly(),
                StringComparer.Ordinal));
    }

    #endregion
}
