namespace AuditTrail.Abstractions;
public interface IAuditTrailDecryption
{
    string? Decrypt(byte[]? cipherText, bool includesHash);
}
