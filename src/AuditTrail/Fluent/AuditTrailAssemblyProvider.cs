using AuditTrail.Fluent.Abstraction;
using System.Security;
using static AuditTrail.Fluent.AssemblyScanner;

namespace AuditTrail.Fluent;
public class AuditTrailAssemblyProvider<TPermission>(IEnumerable<AssemblyScanner> assemblyScaners) : IAuditTrailAssemblyProvider<TPermission>
{
    public IEnumerable<AssemblyScanner> AssemblyScanners { get; } = assemblyScaners;
    public IEnumerable<AssemblyScanResult> AssemblyScanResult => AssemblyScanners.SelectMany(s => s);
}
