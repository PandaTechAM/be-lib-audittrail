using AuditTrail.Abstraction;
using AuditTrail.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AuditTrail.Services;

/// <summary>
/// For Design Time Db Context Factory
/// </summary>
public class AuditTrailServiceMock<TPermission> : IAuditTrailService<TPermission>
{
    public List<AuditTrailCommanModel<TPermission>> AuditTransactionData { get; set; }

    public Task<int> AuditTrialSaveChangesAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public IEnumerable<AuditTrailEntityData<TPermission>> GetEntityTrackedPropertiesBeforeSave(ChangeTracker changeTracker)
    {
        return null!;
    }

    public Task SendToConsumerAsync(IEnumerable<AuditTrailCommanModel<TPermission>> auditTrial, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public IEnumerable<AuditTrailCommanModel<TPermission>> UpdateEntityPropertiesAfterSave(IEnumerable<AuditTrailEntityData<TPermission>> auditEntitiesData, DbContext context)
    {
        return null!;
    }
}
