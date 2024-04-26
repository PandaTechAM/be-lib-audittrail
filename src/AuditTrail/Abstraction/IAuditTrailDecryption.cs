namespace AuditTrail.Abstraction;
public interface IAuditTrailDecryption
{
    string? Decrypt(byte[]? cipherText, bool includesHash);
}
