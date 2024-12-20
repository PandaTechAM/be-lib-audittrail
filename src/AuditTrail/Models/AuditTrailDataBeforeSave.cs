﻿namespace AuditTrail.Models;

public record AuditTrailDataBeforeSave<TPermission>
{
   public Guid UniqueId { get; init; } = Guid.NewGuid();
   public required object Entity { get; init; }
   public required IReadOnlyDictionary<string, object> ModifiedProperties { get; init; }
   public string? EntityId { get; init; }
   public TPermission? RequiredReadPermission { get; init; }
   public required DateTime Timestamp { get; init; }
   public required AuditActionType Action { get; init; }
   public required string EntityName { get; init; }
   public required string DataJson { get; init; }
}