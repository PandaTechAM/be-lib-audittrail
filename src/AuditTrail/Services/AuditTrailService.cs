using AuditTrail.Abstractions;
using AuditTrail.Fluent.Abstractions;
using AuditTrail.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuditTrail.Services;

public sealed class AuditTrailService<TPermission>(
   IAuditTrailConsumer<TPermission> auditTrailConsumer,
   IServiceProvider serviceProvider,
   IAuditTrailAssemblyProvider<TPermission> auditAssemblyProvider,
   IOptions<AuditTrailOptions> options,
   ILogger<AuditTrailService<TPermission>> logger) :
   AuditTrailServiceBase<TPermission>(auditTrailConsumer, serviceProvider, auditAssemblyProvider, options, logger)
{
}

public sealed class AuditTrailService<TPermission, TInstance>(
   IAuditTrailConsumer<TPermission, TInstance> auditTrailConsumer,
   IServiceProvider serviceProvider,
   IAuditTrailAssemblyProvider<TInstance> auditAssemblyProvider,
   IOptions<AuditTrailOptions> options,
   ILogger<AuditTrailService<TPermission>> logger) :
   AuditTrailServiceBase<TPermission>(auditTrailConsumer, serviceProvider, auditAssemblyProvider, options, logger),
   IAuditTrailService<TPermission, TInstance>
{
   private readonly IServiceProvider _serviceProvider1 = serviceProvider;

   protected override object GetService(params Type[] types)
   {
      var openGenericType = typeof(IEntityRule<,,>);
      var requiredTypes = types.ToList();
      requiredTypes.Add(typeof(TInstance));
      var closedGenericType = openGenericType.MakeGenericType(requiredTypes.ToArray());

      var entityRule = _serviceProvider1.GetService(closedGenericType);

      if (entityRule is null)
      {
         throw new ArgumentNullException($"Missing service for rule {closedGenericType.FullName}");
      }

      return entityRule;
   }
}