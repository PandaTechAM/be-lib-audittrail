namespace AuditTrail.Fluent.Abstraction;
public interface IRuleBulder<TEntity, TPermission, TProperty>
    where TEntity : class
{
    IRuleBulder<TEntity, TPermission, TProperty> SetRule(IPropertyRule<TEntity, TProperty> rule);
}