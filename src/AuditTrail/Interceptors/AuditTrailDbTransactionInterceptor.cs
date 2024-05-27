using AuditTrail.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace AuditTrail.Interceptors;
public class AuditTrailDbTransactionInterceptor<TPermission>(IHttpContextAccessor httpContextAccessor) : DbTransactionInterceptor
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

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
        ClearTransactionData();
        base.TransactionRolledBack(transaction, eventData);
    }

    public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        ClearTransactionData();
        return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
    }

    protected virtual IAuditTrailService<TPermission>? GetAuditTrailService(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.RequestServices?.GetService<IAuditTrailService<TPermission>>();
    }

    private Task SendToConsumer()
    {
        var auditTrailService = GetAuditTrailService(httpContextAccessor);

        if (auditTrailService != null)
        {
            return auditTrailService.SendToConsumerAsync();
        }

        return Task.CompletedTask;
    }

    private void ClearTransactionData()
    {
        var auditTrailService = GetAuditTrailService(httpContextAccessor);

        auditTrailService?.ClearTransactionData();
    }
}

public class AuditTrailDbTransactionInterceptor<TPermission, TInstance>(IHttpContextAccessor httpContextAccessor)
    : AuditTrailDbTransactionInterceptor<TPermission>(httpContextAccessor)
{
    protected override IAuditTrailService<TPermission>? GetAuditTrailService(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.RequestServices?.GetService<IAuditTrailService<TPermission, TInstance>>();
    }
}
