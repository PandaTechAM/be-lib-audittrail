using AuditTrail.Fluent.Abstraction;
using static AuditTrail.Fluent.AssemblyScanner;

namespace AuditTrail.Fluent;
public class AuditTrailMultiAssemblyProvider(IEnumerable<AssemblyScanner> assemblyScaners) : IAuditTrailAssemblyProvider
{
    public IEnumerable<AssemblyScanner> AssemblyScanners { get; } = assemblyScaners;
    public IEnumerable<AssemblyScanResult> AssemblyScanResult => AssemblyScanners.SelectMany(s => s);
}
