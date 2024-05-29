using AuditTrail.Abstractions;
using AuditTrail.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace AuditTrail.Interceptors;
public class AuditTrailDbTransactionInterceptor<TPermission>(IHttpContextAccessor httpContextAccessor) : DbTransactionInterceptor
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
    {
        BeforeTransactionCommitted(transaction, eventData).GetAwaiter().GetResult();
        return base.TransactionCommitting(transaction, eventData, result);
    }

    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        await BeforeTransactionCommitted(transaction, eventData, cancellationToken);
        return await base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
    }

    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        TransactionFinishedAsync(eventData, TransactionStatus.Commited).GetAwaiter().GetResult();
        base.TransactionCommitted(transaction, eventData);
    }

    public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await TransactionFinishedAsync(eventData, TransactionStatus.Commited, cancellationToken);
        await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }

    public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData)
    {
        TransactionFinishedAsync(eventData, TransactionStatus.Failed).GetAwaiter().GetResult();
        base.TransactionFailed(transaction, eventData);
    }

    public override async Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        await TransactionFinishedAsync(eventData, TransactionStatus.Failed, cancellationToken);
        await base.TransactionFailedAsync(transaction, eventData, cancellationToken);
    }

    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
    {
        TransactionFinishedAsync(eventData, TransactionStatus.RolledBack).GetAwaiter().GetResult();
        base.TransactionRolledBack(transaction, eventData);
    }

    public override async Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await TransactionFinishedAsync(eventData, TransactionStatus.RolledBack, cancellationToken);
        await base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
    }

    protected virtual IAuditTrailService<TPermission>? GetAuditTrailService(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.RequestServices?.GetService<IAuditTrailService<TPermission>>();
    }

    private Task BeforeTransactionCommitted(DbTransaction transaction, TransactionEventData eventData, CancellationToken cancellationToken = default)
    {
        var auditTrailService = GetAuditTrailService(httpContextAccessor);

        if (auditTrailService != null)
        {
            return auditTrailService.BeforeTransactionCommitedAsync(transaction, eventData, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private Task TransactionFinishedAsync(TransactionEventData eventData, TransactionStatus status, CancellationToken cancellationToken = default)
    {
        var auditTrailService = GetAuditTrailService(httpContextAccessor);

        if (auditTrailService != null)
        {
            return auditTrailService.TransactionFinished(eventData, status);
        }

        return Task.CompletedTask;
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
