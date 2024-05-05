using Microsoft.EntityFrameworkCore;

namespace AuditTrail.Models;
public record AuditTrailEntityData<TPermission>(object auditTrailData, IReadOnlyDictionary<string, object> modifiedProperties, EntityState entityState, string? entityId, TPermission permission)
{
    public object AuditTrailEntity { get; } = auditTrailData;
    public IReadOnlyDictionary<string, object> ModifiedProperties { get; } = modifiedProperties;
    public EntityState EntityState { get; } = entityState;
    public string? EntityId { get; } = entityId;
    public TPermission Permission { get; } = permission;
}
