using AuditTrail.Models;
using System.Threading;

namespace AuditTrail.Abstraction;

public interface IAuditTrailConsumer<TPermission>
{
    Task ConsumeAsync(IEnumerable<AuditTrailDataAfterSave<TPermission>> entities, CancellationToken cancellationToken = default);

    Task BeforeSaveAsync(IEnumerable<AuditTrailDataBeforeSave<TPermission>> entitiesCancellationToken, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public interface IAuditTrailConsumer<TPermission, TInstance>
    : IAuditTrailConsumer<TPermission>
{
}