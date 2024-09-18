using System.Linq.Expressions;
using AuditTrail.Fluent.Abstractions;

namespace AuditTrail.Fluent;

public abstract class EntityRule<TEntity, TPermission> : IEntityRule<TEntity, TPermission> where TEntity : class
{
   private readonly List<IPropertyRule<TEntity>> _rules = [];

   public TPermission? Permission { get; private set; }

   public virtual void ExecuteRules(string propertyName, object? value, Dictionary<string, object?> modifiedProperties)
   {
      var rules = _rules.Where(s => s.PropertyName.Equals(propertyName));

      if (!rules.Any())
      {
         modifiedProperties.Add(propertyName, value);
         return;
      }

      foreach (var rule in rules)
      {
         var result = rule.ExecuteRule(propertyName, value!);
         modifiedProperties.Add(result.Name, result.Value);
      }
   }

   public void SetPermission(TPermission permission)
   {
      Permission = permission;
   }

   public IRuleBuilder<TEntity, TPermission, TProperty> RuleFor<TProperty>(
      Expression<Func<TEntity, TProperty>> expression)
   {
      var rule = PropertyRule<TEntity, TProperty>.Create(expression);
      _rules.Add(rule);
      return new RuleBulder<TEntity, TPermission, TProperty>(rule, this);
   }
}

public abstract class EntityRule<TEntity, TPermission, TInstance> :
   EntityRule<TEntity, TPermission>, IEntityRule<TEntity, TPermission, TInstance>
   where TEntity : class
   where TInstance : class
{
}