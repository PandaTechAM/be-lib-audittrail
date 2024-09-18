using System.Linq.Expressions;
using AuditTrail.Fluent.Abstractions;

namespace AuditTrail.Fluent;

public class PropertyRule<TEntity, TProperty> : IPropertyRule<TEntity, TProperty>
{
   private readonly List<IPropertyRule<TEntity, TProperty>> _propertyRules = new();

   public PropertyRule()
   {
   }

   public PropertyRule(Expression<Func<TEntity, TProperty>> expression)
   {
      var body = expression.Body as MemberExpression
                 ?? throw new ArgumentException("Invalid property");

      PropertyName = body.Member.Name
                     ?? throw new ArgumentException("Invalid property name");
   }

   public string PropertyName { get; } = "";

   public void Add(IPropertyRule<TEntity, TProperty> rule)
   {
      _propertyRules.Add(rule);
   }

   public virtual NameValue ExecuteRule(string name, object value)
   {
      var result = new NameValue(name, value);

      foreach (var item in _propertyRules)
      {
         result = item.ExecuteRule(result.name, result.value!);
      }

      return result;
   }

   public static IPropertyRule<TEntity, TProperty> Create(Expression<Func<TEntity, TProperty>> expression)
   {
      return new PropertyRule<TEntity, TProperty>(expression);
   }
}