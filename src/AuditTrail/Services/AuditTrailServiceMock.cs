using AuditTrail.Abstractions;
using AuditTrail.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuditTrail.Services;

/// <summary>
/// For Design Time Db Context Factory
/// </summary>
public class AuditTrailServiceMock<TPermission> : IAuditTrailService<TPermission>
{
    public Task<int> AuditTrilSaveChangesAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public void ClearSaveData() { }

    public void ClearTransactionData() { }

    public Task FinishSaveChanges(DbContextEventData eventData)
    {
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AuditTrailDataBeforeSave<TPermission>>> GetEntityTrackedPropertiesBeforeSave(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        return null!;
    }

    public Task SendToConsumerAsync(IEnumerable<AuditTrailDataAfterSave<TPermission>> auditTrail, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendToConsumerAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendToTransactionConsumerAsync(TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task StartCollectingSaveData(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public IEnumerable<AuditTrailDataAfterSave<TPermission>> UpdateEntityPropertiesAfterSave(IEnumerable<AuditTrailDataBeforeSave<TPermission>> auditEntitiesData, DbContext context)
    {
        return null!;
    }
}
