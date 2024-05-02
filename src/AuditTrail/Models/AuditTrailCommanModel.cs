namespace AuditTrail.Models;
public class AuditTrailCommanModel<TPermission>
{
    public object Entity { get; set; } = null!;
    public TPermission? RequiredReadPermission { get; set; }
    public DateTime Timestamp { get; set; }
    public AuditActionType Action { get; set; }
    public string EntityName { get; set; } = null!;
    public long? EntityId { get; set; }
    public string DataJson { get; set; } = null!;
    public IReadOnlyDictionary<string, object> ModifiedProperties { get; set; } = null!;
}
