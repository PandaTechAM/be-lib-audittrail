using AuditTrail.Abstraction;
using AuditTrail.Fluent.Abstraction;
using AuditTrail.Fluent.Rules;

namespace AuditTrail.Extenssions;
public static class RuleExtensions
{
    public static IRuleBulder<T, TPermission, TProperty?> Ignore<T, TPermission, TProperty>(this IRuleBulder<T, TPermission, TProperty> ruleBuilder)
        where T : class
    {
        var rule = new IgnorePropertyRule<T, TProperty?>();
        return ruleBuilder.SetRule(rule!)!;
    }
    
    /// <param name="includesHash"> Parameter important to pass exact same value that used to encrypt </param>
    public static IRuleBulder<T, TPermission, TProperty?> Decrypt<T, TPermission, TProperty>(this IRuleBulder<T, TPermission, TProperty> ruleBuilder, IAuditTrailDecryption auditTrailDecryption, bool includesHash)
        where T : class
    {
        var rule = new DecryptPropertyRule<T, TProperty?>(auditTrailDecryption, includesHash);
        return ruleBuilder.SetRule(rule!)!;
    }

    public static IRuleBulder<T, TPermission, TProperty?> ChangeName<T, TPermission, TProperty>(this IRuleBulder<T, TPermission, TProperty> ruleBuilder, string name)
        where T : class
    {
        var rule = new ChangeNamePropertyRule<T, TProperty?>(name);
        return ruleBuilder.SetRule(rule!)!;
    }
}