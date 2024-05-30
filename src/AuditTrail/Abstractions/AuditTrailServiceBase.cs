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
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuditTrail.Abstractions;

public abstract class AuditTrailServiceBase<TPermission> : IAuditTrailService<TPermission>, IDisposable, IAsyncDisposable
{
    private readonly IAuditTrailConsumer<TPermission> _auditTrailConsumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuditTrailAssemblyProvider _auditAssemblyProvider;
    private readonly ILogger<AuditTrailServiceBase<TPermission>> _logger;
    private readonly AuditTrailOptions _options;

    private IDbContextTransaction? _transaction;
    private bool _transactionStarted = false;
    private bool _commitStarted = false;
    private bool _disposed = false;

    private readonly ConcurrentBag<AuditTrailDataAfterSave<TPermission>> _auditTransactionData = new();
    private readonly ConcurrentBag<AuditTrailDataBeforeSave<TPermission>> _auditTrailSaveData = new();

    protected AuditTrailServiceBase(
        IAuditTrailConsumer<TPermission> auditTrailConsumer,
        IServiceProvider serviceProvider,
        IAuditTrailAssemblyProvider auditAssemblyProvider,
        IOptions<AuditTrailOptions> options,
        ILogger<AuditTrailServiceBase<TPermission>> logger = null)
    {
        _auditTrailConsumer = auditTrailConsumer ?? throw new ArgumentNullException(nameof(auditTrailConsumer));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _auditAssemblyProvider = auditAssemblyProvider ?? throw new ArgumentNullException(nameof(auditAssemblyProvider));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        LogDebug("AuditTrailServiceBase initialized");
    }

    protected virtual IEnumerable<EntityEntry?> GetChanges(ChangeTracker changeTracker)
    {
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
            var entityProperties = entityEntry.State == EntityState.Added
                ? GetTrackedPropertiesWithValues(entityEntry.Properties, auditEntity)
                : GetTrackedPropertiesWithValues(entityEntry.Properties.Where(prop => prop.IsModified), auditEntity);

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
            if (entityData.Action == AuditActionType.Create)
            {
                entityId = GetEntityId(entityData.Entity, context.ChangeTracker);
            }

            var auditModel = new AuditTrailDataAfterSave<TPermission>
            {
                UniqueId = entityData.UniqueId,
                Entity = entityData.Entity,
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
        _transactionStarted = false;
        _commitStarted = false;

        if (_auditTransactionData.Any())
        {
            ClearTransactionData();
        }

        await _auditTrailConsumer.TransactionFinished(eventData, status, cancellationToken);
    }

    public async Task FinishSaveChanges(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData?.Context != null && !_commitStarted)
        {
            var updatedData = UpdateEntityPropertiesAfterSave(_auditTrailSaveData, eventData.Context);

            if (eventData.Context.Database.CurrentTransaction == null)
            {
                await SendToConsumerAsync(updatedData);
            }
            else
            {
                foreach (var data in updatedData)
                {
                    _auditTransactionData.Add(data);
                }

                if (_transactionStarted && _transaction != null)
                {
                    try
                    {
                        _commitStarted = true;
                        await _transaction.CommitAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        LogError("Commit transaction failed", ex);
                        await RollbackTransactionAsync();
                    }
                    finally
                    {
                        await DisposeTransactionAsync();
                    }
                }
            }

            ClearSaveData();
        }
    }

    public async Task SavingChangesStartedAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        if (_commitStarted)
        {
            ClearSaveData();
            ClearTransactionData();
            return;
        }

        if (eventData.Context != null)
        {
            if (_options.AutoOpenTransaction && eventData.Context.Database.CurrentTransaction == null)
            {
                _transaction = await eventData.Context.Database.BeginTransactionAsync(cancellationToken);
                _transactionStarted = true;
            }

            var auditData = await GetEntityTrackedPropertiesBeforeSave(eventData, cancellationToken);
            foreach (var data in auditData)
            {
                _auditTrailSaveData.Add(data);
            }
        }
    }

    public async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (_options.AutoOpenTransaction && _transaction != null)
        {
            await RollbackTransactionAsync();
        }
    }

    public Task BeforeTransactionCommitedAsync(DbTransaction transaction, TransactionEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Context?.Database.CurrentTransaction != null && _auditTransactionData.Any())
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
        var ruleService = GetService(entity.GetType(), typeof(TPermission)) as IEntityRule<TPermission>;

        if (ruleService == null) throw new ArgumentNullException($"Missing service for rule {entity.GetType().FullName}");

        var permission = ruleService.Permission;

        foreach (var property in properties)
        {
            if (property.Metadata?.PropertyInfo is null || property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            ruleService.ExecuteRules(property.Metadata.PropertyInfo.Name, property.CurrentValue, modifiedProperties);
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
        if (entityRule == null)
        {
            throw new ArgumentNullException($"Missing service for rule {closedGenericType.FullName}");
        }

        return entityRule;
    }

    protected void LogDebug(string logMessage)
    {
        _logger?.LogDebug(logMessage);
    }

    protected void LogError(string message, Exception exception)
    {
        _logger?.LogError(exception, message);
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
            LogError("Send to audit trail consumer failed", ex);
        }
    }

    private async Task RollbackTransactionAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }
        catch (Exception ex)
        {
            LogError("Rollback transaction failed", ex);
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        _commitStarted = false;
        _transactionStarted = false;
    }

    private void DisposeTransaction()
    {
        if (_transaction != null)
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            DisposeTransaction();
        }

        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return;

        await DisposeTransactionAsync();
        _disposed = true;
    }

    ~AuditTrailServiceBase()
    {
        Dispose(false);
    }
}
