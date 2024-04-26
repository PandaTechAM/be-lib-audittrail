using System.Collections.ObjectModel;

namespace AuditTrail.Models;

public class TrackedPropertiesWithPermission<TPermission>(TPermission? permission, IReadOnlyDictionary<string, object> trackedProperties)
{
    public TPermission? Permission { get; } = permission;
    public readonly IReadOnlyDictionary<string, object> TrackedProperties = trackedProperties;
}

