using Microsoft.EntityFrameworkCore;

namespace AuditTrail.Models;
public class AuditTrailEntityData<TPermission>(object auditTrailData, IReadOnlyDictionary<string, object> modifiedProperties, EntityState entityState, long? entityId, TPermission permission)
{
    public object AuditTrailEntity { get; } = auditTrailData;
    public IReadOnlyDictionary<string, object> ModifiedProperties { get; } = modifiedProperties;
    public EntityState EntityState { get; } = entityState;
    public long? EntityId { get; } = entityId;
    public TPermission Permission { get; } = permission;
}
