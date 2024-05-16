using AuditTrail.Models;

namespace AuditTrail.Abstraction;

public interface IAuditTrailConsumer<TPermission>
{
    Task ConsumeAsync(IEnumerable<AuditTrailCommanModel<TPermission>> entities, CancellationToken cancellationToken = default);
}

public interface IAuditTrailConsumer<TPermission, TInstance>
    : IAuditTrailConsumer<TPermission>
{
}