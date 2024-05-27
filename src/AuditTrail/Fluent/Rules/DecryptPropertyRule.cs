using AuditTrail.Abstractions;

namespace AuditTrail.Fluent.Rules;
public class DecryptPropertyRule<TEntity, TProperty> : PropertyRule<TEntity, TProperty>
{
    private readonly bool _includeHash;
    private readonly IAuditTrailDecryption _auditTrailDecryption;

    public override NameValue ExecuteRule(string name, object value)
    {
        try
        {
            var bytes = (byte[])value;

            var decriptedValue = _auditTrailDecryption.Decrypt(bytes, _includeHash);

            return new NameValue(name, decriptedValue!);
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"Only byte[] type decryption is supported, invalid type: " +
                $"{value.GetType().FullName} entity: " +
                $"{typeof(TEntity).FullName} property: {name}");
        }
    }

    public DecryptPropertyRule(IAuditTrailDecryption auditTrailDecryption, bool includeHash)
    {
        _includeHash = includeHash;
        _auditTrailDecryption = auditTrailDecryption;
    }
}