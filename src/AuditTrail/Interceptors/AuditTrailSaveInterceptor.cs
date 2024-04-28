using AuditTrail.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AuditTrail.Interceptors;
public class AuditTrailSaveInterceptor<TPermission>(IHttpContextAccessor  httpContextAccessor) : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StartCollectingSaveData(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StartCollectingSaveData(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
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

    private void StartCollectingSaveData(DbContextEventData eventData)
    {
        var auditTrailService = GetAuditTrailService(httpContextAccessor);

        auditTrailService?.StartCollectingSaveData(eventData);
    }

    private Task FinishSaveChanges(DbContextEventData eventData)
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

    private IAuditTrailService<TPermission>? GetAuditTrailService(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor?.HttpContext?.RequestServices?.GetService<IAuditTrailService<TPermission>>();
    }
}