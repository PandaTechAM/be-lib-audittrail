using AuditTrail.Abstractions;
using AuditTrail.Fluent.Abstractions;
using AuditTrail.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuditTrail.Services;

public sealed class AuditTrailService<TPermission>(
    IAuditTrailConsumer<TPermission> audtTrailConsumer,
    IServiceProvider serviceProvider,
    IAuditTrailAssemblyProvider<TPermission> auditAssemblyProvider,
    ILogger<AuditTrailService<TPermission>> logger,
    IOptions<AuditTrailOptions> options) :
    AuditTrailServiceBase<TPermission>(audtTrailConsumer, serviceProvider, auditAssemblyProvider, logger, options)
{
}

public sealed class AuditTrailService<TPermission, TInstance>(
    IAuditTrailConsumer<TPermission, TInstance> audtTrailConsumer,
    IServiceProvider serviceProvider,
    IAuditTrailAssemblyProvider<TInstance> auditAssemblyProvider,
    ILogger<AuditTrailService<TPermission>> logger,
    IOptions<AuditTrailOptions> options) :
    AuditTrailServiceBase<TPermission>(audtTrailConsumer, serviceProvider, auditAssemblyProvider, logger, options),
    IAuditTrailService<TPermission, TInstance>
{
    protected override object GetService(params Type[] types)
    {
        var openGenericType = typeof(IEntityRule<,,>);
        var requiredTypes = types.ToList();
        requiredTypes.Add(typeof(TInstance));
        var closedGenericType = openGenericType.MakeGenericType(requiredTypes.ToArray());

        logger.LogDebug($"GetService {closedGenericType.FullName}");

        var entityRule = serviceProvider.GetService(closedGenericType);

        if (entityRule is null)
        {
            throw new ArgumentNullException($"Missing service for rule {closedGenericType.FullName}");
        }

        return entityRule;
    }
}