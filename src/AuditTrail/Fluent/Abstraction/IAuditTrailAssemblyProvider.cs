using static AuditTrail.Fluent.AssemblyScanner;

namespace AuditTrail.Fluent.Abstraction;
public interface IAuditTrailAssemblyProvider<TPermission>
{
    IEnumerable<AssemblyScanResult> AssemblyScanResult { get; }
}
