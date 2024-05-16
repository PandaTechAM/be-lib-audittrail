using AuditTrail.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuditTrail.Abstraction;
public interface IAuditTrailService<TPermission>
{
    Task SendToConsumerAsync(CancellationToken cancellationToken = default);
    IEnumerable<AuditTrailEntityData<TPermission>> GetEntityTrackedPropertiesBeforeSave(ChangeTracker changeTracker);
    IEnumerable<AuditTrailCommanModel<TPermission>> UpdateEntityPropertiesAfterSave(IEnumerable<AuditTrailEntityData<TPermission>> auditEntitiesData,
        DbContext context);
    Task FinishSaveChanges(DbContextEventData eventData);
    void ClearTransactionData();
    void ClearSaveData();
    void StartCollectingSaveData(DbContextEventData eventData);
}

public interface IAuditTrailService<TPermission, TInstance> 
    : IAuditTrailService<TPermission>
{
}

