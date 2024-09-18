namespace AuditTrail.Fluent.Rules;

public class ChangeNamePropertyRule<TEntity, TProperty> : PropertyRule<TEntity, TProperty>
{
   private readonly string _changeName;

   public ChangeNamePropertyRule(string name)
   {
      _changeName = name;
   }

   public override NameValue ExecuteRule(string name, object value)
   {
      return new NameValue(_changeName, value);
   }
}