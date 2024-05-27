using AuditTrail.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuditTrail.Abstractions;
public interface IAuditTrailService<TPermission>
{
    Task SendToConsumerAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditTrailDataBeforeSave<TPermission>>> GetEntityTrackedPropertiesBeforeSave(DbContextEventData eventData, CancellationToken cancellationToken = default);
    IEnumerable<AuditTrailDataAfterSave<TPermission>> UpdateEntityPropertiesAfterSave(IEnumerable<AuditTrailDataBeforeSave<TPermission>> auditEntitiesData,
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

