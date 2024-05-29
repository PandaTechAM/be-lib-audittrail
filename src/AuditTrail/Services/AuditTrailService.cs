using AuditTrail.Abstractions;
using AuditTrail.Fluent.Abstractions;
using AuditTrail.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuditTrail.Services;

public sealed class AuditTrailService<TPermission>(
    IAuditTrailConsumer<TPermission> AudtTrailConsumer,
    IServiceProvider ServiceProvider,
    IAuditTrailAssemblyProvider<TPermission> AuditAssemblyProvider,
    ILogger<AuditTrailService<TPermission>> Logger,
    IOptions<AuditTrailOptions> options) :
    AuditTrailServiceBase<TPermission>(AudtTrailConsumer, ServiceProvider, AuditAssemblyProvider, Logger, options)
{
}

public sealed class AuditTrailService<TPermission, TInstance>(
    IAuditTrailConsumer<TPermission, TInstance> AudtTrailConsumer,
    IServiceProvider ServiceProvider,
    IAuditTrailAssemblyProvider<TInstance> AuditAssemblyProvider,
    ILogger<AuditTrailService<TPermission>> Logger,
    IOptions<AuditTrailOptions> options) :
    AuditTrailServiceBase<TPermission>(AudtTrailConsumer, ServiceProvider, AuditAssemblyProvider, Logger, options),
    IAuditTrailService<TPermission, TInstance>
{
    protected override object GetService(params Type[] types)
    {
        var openGenericType = typeof(IEntityRule<,,>);
        var requiredTypes = types.ToList();
        requiredTypes.Add(typeof(TInstance));
        var closedGenericType = openGenericType.MakeGenericType(requiredTypes.ToArray());

        var entityRule = ServiceProvider.GetService(closedGenericType);

        if (entityRule is null)
        {
            throw new ArgumentNullException($"Missing service for rule {closedGenericType.FullName}");
        }

        return entityRule;
    }
}