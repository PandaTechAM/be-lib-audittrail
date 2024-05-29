namespace AuditTrail.Options;

public class AuditTrailOptions
{
    /// <summary>
    /// Autmatically open transaction for savechanges if no transaction is open
    /// </summary>
    public bool AutoOpenTransaction { get; set; }
}

