using static AuditTrail.Fluent.AssemblyScanner;

namespace AuditTrail.Fluent.Abstractions;

public interface IAuditTrailAssemblyProvider
{
    IEnumerable<AssemblyScanResult> AssemblyScanResult { get; }
}

public interface IAuditTrailAssemblyProvider<TInstance> : IAuditTrailAssemblyProvider
{

}
