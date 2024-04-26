
using AuditTrail.Abstraction;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace AuditTrail.Interceptors;
public class AuditTrailDbTransactionInterceptor<TPermission>(IAuditTrailService<TPermission> auditTrailService) : DbTransactionInterceptor
{
    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        SendToConsumer().GetAwaiter().GetResult();
        base.TransactionCommitted(transaction, eventData);
    }

    public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await SendToConsumer();
        await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }

    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
    {
        ClearAuditData();
        base.TransactionRolledBack(transaction, eventData);
    }

    public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        ClearAuditData();
        return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
    }

    public async Task SendToConsumer()
    {
        await auditTrailService.SendToConsumerAsync(auditTrailService.AuditTransactionData);

        ClearAuditData();
    }

    public void ClearAuditData()
    {
        auditTrailService.AuditTransactionData.Clear();
    }
}
