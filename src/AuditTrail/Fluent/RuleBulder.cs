using AuditTrail.Fluent.Abstraction;

namespace AuditTrail.Fluent;
public class RuleBulder<TEntity, TPermission, TProperty> :
    IRuleBulder<TEntity, TPermission, TProperty> 
    where TEntity : class
{
    public IPropertyRule<TEntity, TProperty> Rule { get; }

    public EntityRule<TEntity, TPermission> Parent { get; }

    public RuleBulder(IPropertyRule<TEntity, TProperty> propertyRule, EntityRule<TEntity, TPermission> parent)
    {
        Rule = propertyRule;
        Parent = parent;
    }

    public IRuleBulder<TEntity, TPermission, TProperty> SetRule(IPropertyRule<TEntity, TProperty> rule)
    {
        Rule.Add(rule);
        return this;
    }
}