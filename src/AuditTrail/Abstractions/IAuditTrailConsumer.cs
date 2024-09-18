using System.Data.Common;
using AuditTrail.Enums;
using AuditTrail.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuditTrail.Abstractions;

public interface IAuditTrailConsumer<TPermission>
{
   Task ConsumeAsync(IEnumerable<AuditTrailDataAfterSave<TPermission>> auditTrailData,
      DbTransaction? transaction,
      TransactionEventData? eventData,
      CancellationToken cancellationToken = default);

   Task TransactionFinished(TransactionEventData dbContextEventData,
      TransactionStatus status,
      CancellationToken cancellationToken = default)
   {
      return Task.CompletedTask;
   }
}

public interface IAuditTrailConsumer<TPermission, TInstance>
   : IAuditTrailConsumer<TPermission>
{
}