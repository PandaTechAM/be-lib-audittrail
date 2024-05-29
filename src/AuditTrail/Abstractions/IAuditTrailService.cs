using AuditTrail.Enums;
using AuditTrail.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace AuditTrail.Abstractions;
public interface IAuditTrailService<TPermission>
{
    Task BeforeTransactionCommitedAsync(DbTransaction transaction, TransactionEventData eventData, CancellationToken cancellationToken = default);
    Task TransactionFinished(TransactionEventData eventData, TransactionStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditTrailDataBeforeSave<TPermission>>> GetEntityTrackedPropertiesBeforeSave(DbContextEventData eventData, CancellationToken cancellationToken = default);
    IEnumerable<AuditTrailDataAfterSave<TPermission>> UpdateEntityPropertiesAfterSave(IEnumerable<AuditTrailDataBeforeSave<TPermission>> auditEntitiesData,
        DbContext context);
    Task FinishSaveChanges(DbContextEventData eventData, CancellationToken cancellationToken = default);
    Task SavingChangesStartedAsync(DbContextEventData eventData, CancellationToken cancellationToken = default);
    void ClearTransactionData();
    void ClearSaveData();
}

public interface IAuditTrailService<TPermission, TInstance>
    : IAuditTrailService<TPermission>
{
}
