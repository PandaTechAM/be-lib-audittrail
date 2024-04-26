using AuditTrail.Abstraction;
using AuditTrail.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuditTrail.Interceptors;
public class AuditTrailSaveInterceptor<TPermission>(IAuditTrailService<TPermission> auditTrialService) : SaveChangesInterceptor
{
    private List<AuditTrailEntityData<TPermission>> auditTrailEntityDatas = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StartCollectingAuditData(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StartCollectingAuditData(eventData);
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
        ClearAuditData();
        base.SaveChangesCanceled(eventData);
    }

    public override Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        ClearAuditData();
        return base.SaveChangesCanceledAsync(eventData, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        ClearAuditData();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        ClearAuditData();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public void StartCollectingAuditData(DbContextEventData eventData)
    {
        if (eventData.Context != null)
        {
            IEnumerable<AuditTrailEntityData<TPermission>> auditData = auditTrialService.GetEntityTrackedPropertiesBeforeSave(eventData.Context.ChangeTracker);
            auditTrailEntityDatas.AddRange(auditData);
        }
    }

    public async Task FinishSaveChanges(DbContextEventData eventData)
    {
        if (eventData.Context != null)
        {
            IEnumerable<AuditTrailCommanModel<TPermission>> updatedData = auditTrialService.UpdateEntityPropertiesAfterSave(auditTrailEntityDatas, eventData.Context);

            if (eventData.Context.Database.CurrentTransaction == null)
            {
                await auditTrialService.SendToConsumerAsync(updatedData);
            }
            else
            {
                auditTrialService.AuditTransactionData.AddRange(updatedData);
            }

            ClearAuditData();
        }
    }

    public void ClearAuditData()
    {
        auditTrailEntityDatas.Clear();
    }
}