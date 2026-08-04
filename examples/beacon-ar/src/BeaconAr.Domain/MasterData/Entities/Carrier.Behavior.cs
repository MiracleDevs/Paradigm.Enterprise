using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Interfaces.MasterData.Entities;
using Paradigm.Enterprise.Domain.Exceptions;

namespace BeaconAr.Domain.MasterData.Entities;

public partial class Carrier
{
    #region Nested Types

    private sealed record ProposedState(string Code, string Name, string ServiceLevel,
        string? TrackingUrlTemplate, bool IsActive);

    #endregion

    #region Public Methods

    public static Carrier Create(CarrierCreateRequest request, int userId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        ProposedState state = Normalize(request.Code, request.Name, request.ServiceLevel,
            request.TrackingUrlTemplate, request.IsActive);
        ValidateState(state);
        return new Carrier
        {
            Code = state.Code,
            Name = state.Name,
            ServiceLevel = state.ServiceLevel,
            TrackingUrlTemplate = state.TrackingUrlTemplate,
            IsActive = state.IsActive,
            CreatedByUserId = userId,
            CreationDate = now,
        };
    }

    public void Replace(CarrierUpdateRequest request, int userId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        ProposedState state = Normalize(request.Code, request.Name, request.ServiceLevel,
            request.TrackingUrlTemplate, request.IsActive);
        ValidateState(state);
        Code = state.Code;
        Name = state.Name;
        ServiceLevel = state.ServiceLevel;
        TrackingUrlTemplate = state.TrackingUrlTemplate;
        IsActive = state.IsActive;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    public void Activate(int userId, DateTimeOffset now)
    {
        IsActive = true;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    public void Deactivate(int userId, DateTimeOffset now)
    {
        IsActive = false;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    #endregion

    #region Private Methods

    partial void ValidateEntity() => ValidateState(Normalize(Code, Name, ServiceLevel, TrackingUrlTemplate, IsActive));

    partial void BeforeMap(ICarrier model)
    {
        _ = Id;
        ValidateState(Normalize(model.Code, model.Name, model.ServiceLevel, model.TrackingUrlTemplate, model.IsActive));
    }

    partial void AfterMap(ICarrier model)
    {
        ProposedState state = Normalize(Code, Name, ServiceLevel, TrackingUrlTemplate, IsActive);
        Code = state.Code;
        Name = state.Name;
        ServiceLevel = state.ServiceLevel;
        TrackingUrlTemplate = state.TrackingUrlTemplate;
    }

    private static ProposedState Normalize(string? code, string? name, string? serviceLevel,
        string? trackingUrlTemplate, bool isActive) => new(
        code?.Trim() ?? string.Empty,
        name?.Trim() ?? string.Empty,
        serviceLevel?.Trim() ?? string.Empty,
        string.IsNullOrWhiteSpace(trackingUrlTemplate) ? null : trackingUrlTemplate.Trim(),
        isActive);

    private static void ValidateState(ProposedState state)
    {
        var rules = new DomainValidator();
        rules.Assert(state.Code.Length is >= 1 and <= 32, "Code is required and cannot exceed 32 characters.");
        rules.Assert(state.Name.Length is >= 1 and <= 120, "Name is required and cannot exceed 120 characters.");
        rules.Assert(state.ServiceLevel.Length is >= 1 and <= 120, "Service level is required and cannot exceed 120 characters.");
        rules.Assert(state.TrackingUrlTemplate is null || state.TrackingUrlTemplate.Length <= 2048 &&
                     IsSafeTrackingTemplate(state.TrackingUrlTemplate),
            "Tracking URL template must be a safe absolute HTTPS URL with only the optional {trackingNumber} placeholder.");
        rules.ThrowIfAny();
    }

    private static bool IsSafeTrackingTemplate(string value)
    {
        const string token = "{trackingNumber}";
        if (value.Any(char.IsControl))
            return false;
        string withoutToken = value.Replace(token, "tracking-number", StringComparison.Ordinal);
        return !withoutToken.Contains('{') && !withoutToken.Contains('}') &&
               Uri.TryCreate(withoutToken, UriKind.Absolute, out Uri? uri) &&
               !string.IsNullOrWhiteSpace(uri.Host) && uri.Scheme == Uri.UriSchemeHttps &&
               string.IsNullOrEmpty(uri.UserInfo);
    }

    #endregion
}
