using static AuditTrail.Fluent.AssemblyScanner;

namespace AuditTrail.Fluent.Abstraction;

public interface IAuditTrailAssemblyProvider
{
    IEnumerable<AssemblyScanResult> AssemblyScanResult { get; }
}

public interface IAuditTrailAssemblyProvider<TInstance> : IAuditTrailAssemblyProvider
{

}
