using AuditTrail.Models;
using System.Threading;

namespace AuditTrail.Abstraction;

public interface IAuditTrailConsumer<TPermission>
{
    Task ConsumeAsync(IEnumerable<AuditTrailCommanModel<TPermission>> entities, CancellationToken cancellationToken = default);

    Task BeforeSaveAsync(IEnumerable<AuditTrailEntityData<TPermission>> entitiesCancellationToken, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public interface IAuditTrailConsumer<TPermission, TInstance>
    : IAuditTrailConsumer<TPermission>
{
}