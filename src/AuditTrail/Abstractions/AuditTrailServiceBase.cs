using AuditTrail.Enums;
using AuditTrail.Fluent.Abstractions;
using AuditTrail.Models;
using AuditTrail.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Data.Common;
using System.Reflection;
using System.Security;

namespace AuditTrail.Abstractions;

public abstract class AuditTrailServiceBase<TPermission> : IAuditTrailService<TPermission>
{
    private readonly IAuditTrailConsumer<TPermission> _auditTrailConsumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuditTrailAssemblyProvider _auditAssemblyProvider;
    private readonly ILogger<AuditTrailServiceBase<TPermission>> _logger;
    private readonly AuditTrailOptions _options;
    private bool transactionStarted = false;
    private bool commitStarted = false;

    private readonly List<AuditTrailDataAfterSave<TPermission>> _auditTransactionData = new();
    private readonly List<AuditTrailDataBeforeSave<TPermission>> _auditTrailSaveData = new();

    protected AuditTrailServiceBase(
        IAuditTrailConsumer<TPermission> auditTrailConsumer,
        IServiceProvider serviceProvider,
        IAuditTrailAssemblyProvider auditAssemblyProvider,
        IOptions<AuditTrailOptions> options,
        ILogger<AuditTrailServiceBase<TPermission>> logger = null)
    {
        LogDebug("AuditTrailServiceBase constructor");

        _auditTrailConsumer = auditTrailConsumer ?? throw new ArgumentNullException(nameof(auditTrailConsumer));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _auditAssemblyProvider = auditAssemblyProvider ?? throw new ArgumentNullException(nameof(auditAssemblyProvider));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    protected virtual IEnumerable<EntityEntry?> GetChanges(ChangeTracker changeTracker)
    {
        LogDebug("GetChanges");

        var trackedEntityTypes = _auditAssemblyProvider.AssemblyScanResult
            .Select(s => s.InterfaceType.GetGenericArguments()[0])
            .ToList();

        return changeTracker.Entries()
            .Where(e => (e.State == EntityState.Modified || e.State == EntityState.Added || e.State == EntityState.Deleted)
                        && trackedEntityTypes.Contains(e.Entity.GetType()));
    }

    public async Task<IEnumerable<AuditTrailDataBeforeSave<TPermission>>> GetEntityTrackedPropertiesBeforeSave(
        DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        var auditEntities = new List<AuditTrailDataBeforeSave<TPermission>>();

        if (_auditAssemblyProvider.AssemblyScanResult is null || eventData.Context is null)
        {
            return auditEntities;
        }

        var changes = GetChanges(eventData.Context.ChangeTracker);

        foreach (var entityEntry in changes.Where(s => s?.Entity != null))
        {
            var auditEntity = entityEntry!.Entity;

            TrackedPropertiesWithPermission<TPermission> entityProperties;
            if (entityEntry.State == EntityState.Added)
            {
                entityProperties = GetTrackedPropertiesWithValues(entityEntry.Properties, auditEntity);
            }
            else
            {
                entityProperties = GetTrackedPropertiesWithValues(entityEntry.Properties.Where(prop => prop.IsModified), auditEntity);
            }

            // For deleted entities, we can't dynamically get id/id's, so we need to get it before SaveChanges
            var id = GetEntityId(auditEntity, eventData.Context.ChangeTracker);

            var auditData = new AuditTrailDataBeforeSave<TPermission>
            {
                Entity = entityEntry.Entity,
                RequiredReadPermission = entityProperties.Permission,
                EntityId = id,
                EntityName = entityEntry.GetType().GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? entityEntry.Entity.GetType().Name,
                Action = GetActionFromEntityState(entityEntry.State),
                DataJson = System.Text.Json.JsonSerializer.Serialize(entityProperties.TrackedProperties),
                Timestamp = DateTime.UtcNow,
                ModifiedProperties = entityProperties.TrackedProperties,
            };

            auditEntities.Add(auditData);
        }

        return auditEntities;
    }

    public IEnumerable<AuditTrailDataAfterSave<TPermission>> UpdateEntityPropertiesAfterSave(
        IEnumerable<AuditTrailDataBeforeSave<TPermission>> auditEntitiesData,
        DbContext context)
    {
        var auditEntitiesUpdatedData = new List<AuditTrailDataAfterSave<TPermission>>();
        foreach (var entityData in auditEntitiesData)
        {
            var entityId = entityData.EntityId;
            var entity = entityData.Entity;
            if (entityData.Action == AuditActionType.Create)
            {
                // For new added entities, we need to update the EntityId with the new Id/id's after SaveChanges
                entityId = GetEntityId(entity, context.ChangeTracker);
            }

            var auditModel = new AuditTrailDataAfterSave<TPermission>
            {
                UniqueId = entityData.UniqueId,
                Entity = entity,
                RequiredReadPermission = entityData.RequiredReadPermission,
                EntityId = entityId,
                EntityName = entityData.EntityName,
                Action = entityData.Action,
                DataJson = entityData.DataJson,
                Timestamp = entityData.Timestamp,
                ModifiedProperties = entityData.ModifiedProperties,
            };
            auditEntitiesUpdatedData.Add(auditModel);
        }

        return auditEntitiesUpdatedData;
    }

    public async Task TransactionFinished(TransactionEventData eventData, TransactionStatus status, CancellationToken cancellationToken = default)
    {
        LogDebug("TransactionFinished");

        transactionStarted = false;
        commitStarted = false;

        if (_auditTransactionData.Any())
        {
            ClearTransactionData();
        }

        await _auditTrailConsumer.TransactionFinished(eventData, status, cancellationToken);
    }

    public async Task FinishSaveChanges(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        LogDebug("FinishSaveChanges");

        if (eventData?.Context != null && !commitStarted)
        {
            var updatedData = UpdateEntityPropertiesAfterSave(_auditTrailSaveData, eventData.Context);

            if (eventData.Context.Database.CurrentTransaction == null)
            {
                await SendToConsumerAsync(updatedData);
            }
            else
            {
                _auditTransactionData.AddRange(updatedData);

                if (transactionStarted && eventData.Context != null && eventData.Context.Database.CurrentTransaction != null)
                {
                    try
                    {
                        commitStarted = true;
                        await eventData.Context.Database.CommitTransactionAsync(cancellationToken);
                    }
                    finally
                    {
                        transactionStarted = false;
                        commitStarted = false;
                    }
                }
            }

            ClearSaveData();
        }
    }

    public async Task SavingChangesStartedAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        LogDebug("SavingChangesStartedAsync");

        if (commitStarted)
        {
            ClearSaveData();
            ClearTransactionData();
            return;
        }

        if (eventData.Context != null)
        {
            if (_options.AutoOpenTransaction && eventData.Context != null && eventData.Context.Database.CurrentTransaction == null)
            {
                await eventData.Context.Database.BeginTransactionAsync(cancellationToken);
                transactionStarted = true;
            }

            var auditData = await GetEntityTrackedPropertiesBeforeSave(eventData, cancellationToken);
            _auditTrailSaveData.AddRange(auditData);
        }
    }

    public Task BeforeTransactionCommitedAsync(DbTransaction transaction, TransactionEventData eventData, CancellationToken cancellationToken = default)
    {
        LogDebug("BeforeTransactionCommitedAsync");

        if (eventData.Context?.Database.CurrentTransaction != null && eventData.Context != null && _auditTransactionData.Any())
        {
            return _auditTrailConsumer.BeforeTransactionCommitedAsync(_auditTransactionData, transaction, eventData, cancellationToken);
        }

        return Task.CompletedTask;
    }

    public void ClearTransactionData()
    {
        _auditTransactionData.Clear();
    }

    public void ClearSaveData()
    {
        _auditTrailSaveData.Clear();
    }

    protected string? GetEntityId(object entity, ChangeTracker changeTracker)
    {
        LogDebug("GetEntityId");

        return changeTracker.Entries()
            .FirstOrDefault(e => e.Entity == entity)?
            .Properties
            .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?
            .CurrentValue?
            .ToString();
    }

    protected TrackedPropertiesWithPermission<TPermission> GetTrackedPropertiesWithValues(
        IEnumerable<PropertyEntry> properties, object entity)
    {
        var modifiedProperties = new Dictionary<string, object>();

        var ruleService = GetService(entity.GetType(), typeof(TPermission));

        var entityRule = ruleService as IEntityRule<TPermission>;
        var permission = entityRule!.Permission;

        foreach (var property in properties)
        {
            if (property.Metadata?.PropertyInfo is null || property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            entityRule.ExecuteRules(property.Metadata.PropertyInfo.Name, property.CurrentValue, modifiedProperties);
        }

        return new TrackedPropertiesWithPermission<TPermission>(permission, modifiedProperties.AsReadOnly());
    }

    protected AuditActionType GetActionFromEntityState(EntityState entityState)
    {
        return entityState switch
        {
            EntityState.Added => AuditActionType.Create,
            EntityState.Modified => AuditActionType.Update,
            EntityState.Deleted => AuditActionType.Delete,
            _ => throw new SecurityException("Invalid Entity State"),
        };
    }

    protected virtual object GetService(params Type[] types)
    {
        var openGenericType = typeof(IEntityRule<,>);
        var closedGenericType = openGenericType.MakeGenericType(types);

        LogDebug($"GetService {closedGenericType.FullName}");

        var entityRule = _serviceProvider.GetService(closedGenericType);

        if (entityRule is null)
        {
            throw new ArgumentNullException($"Missing service for rule {closedGenericType.FullName}");
        }

        return entityRule;
    }

    protected void LogDebug(string logMessage)
    {
        if (_logger != null)
        {
            _logger.LogDebug(logMessage);
        }
    }

    private async Task SendToConsumerAsync(IEnumerable<AuditTrailDataAfterSave<TPermission>> auditTrailData, CancellationToken cancellationToken = default)
    {
        try
        {
            if (auditTrailData.Any())
            {
                await _auditTrailConsumer.ConsumeAsync(auditTrailData, cancellationToken);
                ClearTransactionData();
            }
        }
        catch (Exception ex)
        {
            if (_logger != null)
            {
                _logger.LogError(ex, "Send to audit trail consumer failed");
            }
        }
    }
}