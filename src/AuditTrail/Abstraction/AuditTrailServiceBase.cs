﻿using AuditTrail.Fluent.Abstraction;
using AuditTrail.Models;
using AuditTrail.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Reflection;
using System.Security;

namespace AuditTrail.Abstraction;
public abstract class AuditTrailServiceBase<TPermission> : IAuditTrailService<TPermission>
{
    private readonly IAuditTrailConsumer<TPermission> _auditTrailConsumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuditTrailAssemblyProvider _auditAssemblyProvider;
    private readonly ILogger<AuditTrailServiceBase<TPermission>> _logger;

    private readonly List<AuditTrailCommanModel<TPermission>> _auditTransactionData = [];
    private readonly List<AuditTrailEntityData<TPermission>> _auditTrailSaveData = [];

    protected AuditTrailServiceBase(IAuditTrailConsumer<TPermission> audtTrailConsumer, IServiceProvider serviceProvider, IAuditTrailAssemblyProvider auditAssemblyProvider, ILogger<AuditTrailService<TPermission>> logger)
    {
        _auditTrailConsumer = audtTrailConsumer ?? throw new ArgumentNullException(nameof(audtTrailConsumer));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _auditAssemblyProvider = auditAssemblyProvider ?? throw new ArgumentNullException(nameof(auditAssemblyProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected virtual IEnumerable<EntityEntry?> GetChanges(ChangeTracker changeTracker)
    {
        var changes = changeTracker.Entries()
            .Where(e => e.State is EntityState.Modified or EntityState.Added or EntityState.Deleted
            && _auditAssemblyProvider.AssemblyScanResult
            .Select(s => s.InterfaceType.GetGenericArguments()[0])
            .Contains(e.Entity.GetType()));

        return changes;
    }

    public IEnumerable<AuditTrailEntityData<TPermission>> GetEntityTrackedPropertiesBeforeSave(ChangeTracker changeTracker)
    {
        List<AuditTrailEntityData<TPermission>> auditEntities = [];

        if (_auditAssemblyProvider.AssemblyScanResult is null)
        {
            return auditEntities;
        }

        var changes = GetChanges(changeTracker);

        foreach (var entity in changes)
        {
            var auditEntity = entity!.Entity;

            TrackedPropertiesWithPermission<TPermission> entityProperies;
            if (entity.State == EntityState.Added)
            {
                entityProperies = GetTrackedPropertiesWithValues(entity.Properties, auditEntity);
            }
            else
            {
                entityProperies = GetTrackedPropertiesWithValues(entity.Properties.Where(prop => prop.IsModified), auditEntity);
            }

            // For deleted entities, we can't dinamically get id/id's, so we need to get it before SaveChanges
            var id = GetEntityId(auditEntity, changeTracker);
            auditEntities.Add(new AuditTrailEntityData<TPermission>(auditEntity, entityProperies.TrackedProperties, entity.State, id, entityProperies.Permission));
        }

        return auditEntities;
    }

    public IEnumerable<AuditTrailCommanModel<TPermission>> UpdateEntityPropertiesAfterSave(IEnumerable<AuditTrailEntityData<TPermission>> auditEntitiesData,
        DbContext context)
    {
        var auditEntitiesUpdatedData = new List<AuditTrailCommanModel<TPermission>>();
        foreach (var auditEntityData in auditEntitiesData)
        {
            var entityId = auditEntityData.EntityId;
            var entity = auditEntityData.AuditTrailEntity;
            if (auditEntityData.EntityState == EntityState.Added)
            {
                // For new added entities, we need to update the EntityId with the new Id/id's after SaveChanges
                entityId = GetEntityId(entity, context.ChangeTracker);
            }

            var auditModel = new AuditTrailCommanModel<TPermission>
            {
                Entity = entity,
                RequiredReadPermission = auditEntityData.Permission,
                EntityId = entityId,
                EntityName = entity.GetType().GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? auditEntityData.AuditTrailEntity.GetType().Name,
                Action = GetActionFromEntityState(auditEntityData.EntityState),
                DataJson = System.Text.Json.JsonSerializer.Serialize(auditEntityData.ModifiedProperties),
                Timestamp = DateTime.UtcNow,
                ModifiedProperties = auditEntityData.ModifiedProperties,
            };
            auditEntitiesUpdatedData.Add(auditModel);
        }

        return auditEntitiesUpdatedData;
    }

    public Task SendToConsumerAsync(CancellationToken cancellationToken = default)
    {
        return SendToConsumerAsync(_auditTransactionData, cancellationToken);
    }

    public async Task FinishSaveChanges(DbContextEventData eventData)
    {
        if (eventData?.Context != null)
        {
            var updatedData = UpdateEntityPropertiesAfterSave(_auditTrailSaveData, eventData.Context);

            if (eventData.Context.Database.CurrentTransaction == null)
            {
                await SendToConsumerAsync(updatedData);
            }
            else
            {
                _auditTransactionData.AddRange(updatedData);
            }

            ClearSaveData();
        }
    }

    public void StartCollectingSaveData(DbContextEventData eventData)
    {
        if (eventData?.Context != null)
        {
            var auditData = GetEntityTrackedPropertiesBeforeSave(eventData.Context.ChangeTracker);
            _auditTrailSaveData.AddRange(auditData);
        }
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
        return changeTracker.Entries()?
        .FirstOrDefault(e => e.Entity == entity)?
        .Properties?
        .First(p => p?.Metadata?.IsPrimaryKey() ?? false)?
        .CurrentValue?
        .ToString();
    }

    protected TrackedPropertiesWithPermission<TPermission> GetTrackedPropertiesWithValues(IEnumerable<PropertyEntry> properties, object entity)
    {
        var modifiedProperties = new Dictionary<string, object>();

        var ruleService = GetService(entity.GetType(), typeof(TPermission));

        var entityRule = ruleService as IEntityRule<TPermission>;
        TPermission permission = entityRule!.Permission!;

        foreach (PropertyEntry property in properties)
        {
            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            entityRule.ExecuteRules(property.Metadata.PropertyInfo!.Name, property.CurrentValue!, modifiedProperties);
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

        var entityRule = _serviceProvider.GetService(closedGenericType);

        if (entityRule is null)
        {
            throw new ArgumentNullException($"Missing service for rule {closedGenericType.FullName}");
        }
        return entityRule;
    }

    private async Task SendToConsumerAsync(IEnumerable<AuditTrailCommanModel<TPermission>> auditTraildata, CancellationToken cancellationToken = default)
    {
        try
        {
            if (auditTraildata.Any())
            {
                await _auditTrailConsumer.ConsumeAsync(auditTraildata, cancellationToken);
                ClearTransactionData();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Send to audittrailconsumer failed {ex}");
        }
    }
}