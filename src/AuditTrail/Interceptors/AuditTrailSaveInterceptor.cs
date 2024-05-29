﻿using AuditTrail.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AuditTrail.Interceptors;
public class AuditTrailSaveInterceptor<TPermission>(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        SavingChangesStartedAsync(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await SavingChangesStartedAsync(eventData, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await FinishSaveChanges(eventData);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        FinishSaveChanges(eventData).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override void SaveChangesCanceled(DbContextEventData eventData)
    {
        ClearSaveData();
        base.SaveChangesCanceled(eventData);
    }

    public override Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        ClearSaveData();
        return base.SaveChangesCanceledAsync(eventData, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        ClearSaveData();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        ClearSaveData();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    protected virtual IAuditTrailService<TPermission>? GetAuditTrailService(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.RequestServices?.GetService<IAuditTrailService<TPermission>>();
    }

    private Task SavingChangesStartedAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        var auditTrailService = GetAuditTrailService(httpContextAccessor);

        return auditTrailService?.SavingChangesStartedAsync(eventData, cancellationToken);
    }

    private Task FinishSaveChanges(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        var auditTrailService = GetAuditTrailService(httpContextAccessor);

        if (auditTrailService != null)
        {
            return auditTrailService.FinishSaveChanges(eventData);
        }

        return Task.CompletedTask;
    }

    private void ClearSaveData()
    {
        var auditTrailService = GetAuditTrailService(httpContextAccessor);

        auditTrailService?.ClearSaveData();
    }
}

public class AuditTrailSaveInterceptor<TPermission, TInstance>(IHttpContextAccessor httpContextAccessor)
    : AuditTrailSaveInterceptor<TPermission>(httpContextAccessor)
{
    protected override IAuditTrailService<TPermission>? GetAuditTrailService(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.RequestServices?.GetService<IAuditTrailService<TPermission, TInstance>>();
    }
}
