using AuditTrail.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AuditTrail.Abstraction;
public interface IAuditTrailService<TPermission>
{
    public List<AuditTrailCommanModel<TPermission>> AuditTransactionData { get; set; }
    Task SendToConsumerAsync(IEnumerable<AuditTrailCommanModel<TPermission>> auditTrial, CancellationToken cancellationToken = default);
    IEnumerable<AuditTrailEntityData<TPermission>> GetEntityTrackedPropertiesBeforeSave(ChangeTracker changeTracker);
    IEnumerable<AuditTrailCommanModel<TPermission>> UpdateEntityPropertiesAfterSave(IEnumerable<AuditTrailEntityData<TPermission>> auditEntitiesData,
        DbContext context);
}
