using BeaconAr.Interfaces.Operations.Entities;
using Paradigm.Enterprise.Domain.Exceptions;
using System.Text.Json;

namespace BeaconAr.Domain.Operations.Entities;

public partial class AuditLog
{
    #region Public Methods

    public static AuditLog Create(string resourceType, string resourceId, string action, int userId,
        DateTimeOffset recordedAt, string correlationId, string? previousStatusCode = null,
        string? newStatusCode = null, string? metadataJson = null)
    {
        var value = new AuditLog
        {
            ResourceType = Required(resourceType),
            ResourceId = Required(resourceId),
            Action = Required(action),
            UserId = userId,
            RecordedAt = recordedAt,
            CorrelationId = Required(correlationId),
            PreviousStatusCode = Optional(previousStatusCode),
            NewStatusCode = Optional(newStatusCode),
            MetadataJson = Optional(metadataJson),
        };
        value.Validate();
        return value;
    }

    #endregion

    #region Private Methods

    partial void ValidateEntity()
    {
        var rules = new DomainValidator();
        rules.Assert(ResourceType?.Trim().Length is >= 1 and <= 100, "Audit resource type is required and cannot exceed 100 characters.");
        rules.Assert(ResourceId?.Trim().Length is >= 1 and <= 50, "Audit resource ID is required and cannot exceed 50 characters.");
        rules.Assert(Action?.Trim().Length is >= 1 and <= 100, "Audit action is required and cannot exceed 100 characters.");
        rules.Assert(UserId > 0, "Audit user ID must be positive.");
        rules.Assert(RecordedAt != default, "Audit recorded time is required.");
        rules.Assert(CorrelationId?.Trim().Length is >= 1 and <= 128, "Audit correlation ID is required and cannot exceed 128 characters.");
        rules.Assert(PreviousStatusCode is null || PreviousStatusCode.Length <= 32, "Previous status code cannot exceed 32 characters.");
        rules.Assert(NewStatusCode is null || NewStatusCode.Length <= 32, "New status code cannot exceed 32 characters.");
        rules.Assert(MetadataJson is null || MetadataJson.Length <= 4000, "Audit metadata cannot exceed 4,000 characters.");
        rules.Assert(IsJson(MetadataJson), "Audit metadata must be valid JSON when supplied.");
        rules.ThrowIfAny();
    }

    partial void BeforeMap(IAuditLog model)
    {
        _ = Id;
        _ = Create(model.ResourceType, model.ResourceId, model.Action, model.UserId, model.RecordedAt,
            model.CorrelationId, model.PreviousStatusCode, model.NewStatusCode, model.MetadataJson);
    }

    partial void AfterMap(IAuditLog model)
    {
        ResourceType = Required(ResourceType);
        ResourceId = Required(ResourceId);
        Action = Required(Action);
        CorrelationId = Required(CorrelationId);
        PreviousStatusCode = Optional(PreviousStatusCode);
        NewStatusCode = Optional(NewStatusCode);
        MetadataJson = Optional(MetadataJson);
    }

    private static string Required(string? value) => value?.Trim() ?? string.Empty;

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsJson(string? value)
    {
        if (value is null)
            return true;

        try
        {
            using JsonDocument document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    #endregion
}
