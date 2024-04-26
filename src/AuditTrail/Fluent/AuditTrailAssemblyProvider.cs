using AuditTrail.Fluent.Abstraction;
using static AuditTrail.Fluent.AssemblyScanner;

namespace AuditTrail.Fluent;
public class AuditTrailAssemblyProvider(IEnumerable<AssemblyScanResult> assemblyScanResult) : IAuditTrailAssemblyProvider
{
    public IEnumerable<AssemblyScanResult> AssemblyScanResult { get; } = assemblyScanResult;
}
