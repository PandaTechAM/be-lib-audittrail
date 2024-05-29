using AuditTrail.Enums;
using AuditTrail.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace AuditTrail.Abstractions;

public interface IAuditTrailConsumer<TPermission>
{
    Task ConsumeAsync(IEnumerable<AuditTrailDataAfterSave<TPermission>> auditTrailData, CancellationToken cancellationToken = default);

    Task BeforeTransactionCommitedAsync(IEnumerable<AuditTrailDataAfterSave<TPermission>> auditTrailData, DbTransaction transaction, TransactionEventData eventData, CancellationToken cancellationToken = default);

    Task TransactionFinished(TransactionEventData dbContextEventData, TransactionStatus status, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public interface IAuditTrailConsumer<TPermission, TInstance>
    : IAuditTrailConsumer<TPermission>
{
}