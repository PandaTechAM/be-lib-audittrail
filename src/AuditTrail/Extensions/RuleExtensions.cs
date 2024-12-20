﻿using AuditTrail.Abstractions;
using AuditTrail.Fluent.Abstractions;
using AuditTrail.Fluent.Rules;

namespace AuditTrail.Extensions;

public static class RuleExtensions
{
   public static IRuleBuilder<T, TPermission, TProperty?> Ignore<T, TPermission, TProperty>(
      this IRuleBuilder<T, TPermission, TProperty> ruleBuilder)
      where T : class
   {
      var rule = new IgnorePropertyRule<T, TProperty?>();
      return ruleBuilder.SetRule(rule!)!;
   }

   /// <param name="includesHash"> Parameter important to pass exact same value that used to encrypt </param>
   public static IRuleBuilder<T, TPermission, TProperty?> Decrypt<T, TPermission, TProperty>(
      this IRuleBuilder<T, TPermission, TProperty> ruleBuilder,
      IAuditTrailDecryption auditTrailDecryption,
      bool includesHash)
      where T : class
   {
      var rule = new DecryptPropertyRule<T, TProperty?>(auditTrailDecryption, includesHash);
      return ruleBuilder.SetRule(rule!)!;
   }

   public static IRuleBuilder<T, TPermission, TProperty?> ChangeName<T, TPermission, TProperty>(
      this IRuleBuilder<T, TPermission, TProperty> ruleBuilder,
      string name)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(name);
      var rule = new ChangeNamePropertyRule<T, TProperty?>(name);
      return ruleBuilder.SetRule(rule!)!;
   }

   public static IRuleBuilder<T, TPermission, TProperty?> ChangeValue<T, TPermission, TProperty>(
      this IRuleBuilder<T, TPermission, TProperty?> ruleBuilder,
      Func<TProperty?, object> func)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(func);
      return ruleBuilder.SetRule(new ChangeValuePropertyRule<T, TProperty>(property => func(property)));
   }
}