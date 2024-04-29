# Pandatech.***

## Introduction
Pandatech.AuditTrail is a tool meticulously crafted to gather vital entity data from the change tracker post DbContext SaveChanges operation.

## Features
Audits necessary entities for logging or preserving modified property data.
Offers a convenient fluent configuration to specify tracked entities with ease.

## Installation
Install the package via NuGet Package Manager or use the following command:

```bash
Install-Package Pandatech.AuditTrail
```
## Configuration
1. Implement the `IAuditTrailConsumer` interface.
To be able to retrive modified entities data after save operation.
```csharp
public class AuditTrailConsumer<TPermission>() : IAuditTrailConsumer<TPermission>
{
   public Task ConsumeAsync(IEnumerable<AuditTrailCommanModel<TPermission>> entities, CancellationToken cancellationToken = default)
   {
   }
}
```

2. Add required services.

```csharp
builder.Services.AddAuditTrailFromAssembly(typeof(AssemblyReference).Assembly);
builder.Services.AddScoped<IAuditTrailService<PermissionType?>, AuditTrailService<PermissionType?>>();
builder.Services.AddScoped<IAuditTrailConsumer<PermissionType?>, AuditTrailConsumer<PermissionType?>>();
//Optional only for properties that encrypted to byte[] and require Decryption.
builder.Services.AddScoped<IAuditTrailDecryption, AuditTrailDecryption>(); 

services.AddSingleton<AuditTrailSaveInterceptor<PermissionType?>>();
services.AddSingleton<AuditTrailDbTransactionInterceptor<PermissionType?>>();

services.AddDbContextPool<PostgresContext>((sp, options) =>
{
   var auditSaveInterceptor = sp.GetRequiredService<AuditTrailSaveInterceptor<PermissionType?>>();
   var auditDbTransactionInterceptor = sp.GetRequiredService<AuditTrailDbTransactionInterceptor<PermissionType?>>();

   options.UseNpgsql(connectionString)
   .AddInterceptors(auditSaveInterceptor)
   .AddInterceptors(auditDbTransactionInterceptor);
});
```

## Usage

Create a rule derived from `EntityRule`.

```csharp
public class UserRule : EntityRule<UserEntity, PermissionType?>
{
   public UserRule(ISomeService someService)
   {
      SetPermission(PermissionType.Users_Read);

      RuleFor(s => s.PasswordHash).Ignore();
      RuleFor(s => s.Status).ChangeName("UserStatus");
   }
}
```

You can create your own rules and modify properties as needed.
Custom rule example:
```csharp
public class ChangeNamePropertyRule<TEntity, TProperty> : PropertyRule<TEntity, TProperty>
{
    private readonly string _changeName;

    public override NameValue ExecuteRule(string name, object value)
    {
        return new NameValue(_changeName, value);
    }

    public ChangeNamePropertyRule(string name)
    {
        _changeName = name;
    }
}

public static class RuleExtensions
{
    public static IRuleBulder<T, TPermission, TProperty?> ChangeName<T, TPermission, TProperty>(this IRuleBulder<T, TPermission, TProperty> ruleBuilder, string name)
        where T : class
    {
        var rule = new ChangeNamePropertyRule<T, TProperty?>(name);
        return ruleBuilder.SetRule(rule!)!;
    }
}
```

## Limitations

Only numeric entity IDs are assigned during SaveChanges.
Composite keys are not supported.

## License

Pandatech.*** is licensed under the MIT License.
