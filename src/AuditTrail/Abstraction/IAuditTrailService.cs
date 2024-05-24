using AuditTrail.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuditTrail.Abstraction;
public interface IAuditTrailService<TPermission>
{
    Task SendToConsumerAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditTrailEntityData<TPermission>>> GetEntityTrackedPropertiesBeforeSave(ChangeTracker changeTracker, CancellationToken cancellationToken = default);
    IEnumerable<AuditTrailCommanModel<TPermission>> UpdateEntityPropertiesAfterSave(IEnumerable<AuditTrailEntityData<TPermission>> auditEntitiesData,
        DbContext context);
    Task FinishSaveChanges(DbContextEventData eventData);
    void ClearTransactionData();
    void ClearSaveData();
    Task StartCollectingSaveData(DbContextEventData eventData, CancellationToken cancellationToken = default);
}

public interface IAuditTrailService<TPermission, TInstance> 
    : IAuditTrailService<TPermission>
{
}

