namespace AuditTrail.Fluent.Abstractions;
public interface IRuleBulder<TEntity, TPermission, TProperty>
    where TEntity : class
{
    IRuleBulder<TEntity, TPermission, TProperty> SetRule(IPropertyRule<TEntity, TProperty> rule);
}