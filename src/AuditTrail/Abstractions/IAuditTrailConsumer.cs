using AuditTrail.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuditTrail.Abstractions;

public interface IAuditTrailConsumer<TPermission>
{
    Task ConsumeAsync(IEnumerable<AuditTrailDataAfterSave<TPermission>> entities, CancellationToken cancellationToken = default);

    Task ConsumeTransactionAsync(IEnumerable<AuditTrailDataAfterSave<TPermission>> entities, TransactionEndEventData dbContextEventData, CancellationToken cancellationToken = default);

    Task BeforeSaveAsync(IEnumerable<AuditTrailDataBeforeSave<TPermission>> entitiesCancellationToken, DbContextEventData eventData, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public interface IAuditTrailConsumer<TPermission, TInstance>
    : IAuditTrailConsumer<TPermission>
{
}