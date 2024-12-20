﻿using System.Reflection;
using AuditTrail.Abstractions;
using AuditTrail.Fluent;
using AuditTrail.Fluent.Abstractions;
using AuditTrail.Interceptors;
using AuditTrail.Options;
using AuditTrail.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AuditTrail.Extensions;

public static class ServiceExtensions
{
   /// <summary>
   ///    Add AddAuditTrail which includes AuditTrail services
   /// </summary>
   /// <param name="TPermission">Type taht will be set in rule configuration for each entity rule</param>
   /// <param name="TConsumer">The implementation of IAuditTrailConsumer interface</param>
   /// <param name="services">The collection of services</param>
   /// <param name="assembly">The assembly to scan</param>
   /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
   /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
   /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
   /// <returns>AssemblyScanner</returns>
   public static IServiceCollection AddAuditTrail<TPermission, TConsumer>(this IServiceCollection services,
      Assembly assembly,
      Action<AuditTrailOptions>? options = null,
      ServiceLifetime lifetime = ServiceLifetime.Scoped,
      Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
      bool includeInternalTypes = false)
      where TConsumer : class, IAuditTrailConsumer<TPermission>
   {
      services.AddAuditTrail<TPermission, TConsumer>([assembly], options, lifetime, filter, includeInternalTypes);

      return services;
   }

   /// <summary>
   ///    Add AddAuditTrail which includes AuditTrail services
   /// </summary>
   /// <param name="TPermission">Type taht will be set in rule configuration for each entity rule</param>
   /// <param name="TConsumer">The implementation of IAuditTrailConsumer interface</param>
   /// <param name="services">The collection of services</param>
   /// <param name="assemblies">The assemblies to scan</param>
   /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
   /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
   /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
   /// <returns>AssemblyScanner</returns>
   public static IServiceCollection AddAuditTrail<TPermission, TConsumer>(this IServiceCollection services,
      IEnumerable<Assembly> assemblies,
      Action<AuditTrailOptions>? options = null,
      ServiceLifetime lifetime = ServiceLifetime.Scoped,
      Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
      bool includeInternalTypes = false)
      where TConsumer : class, IAuditTrailConsumer<TPermission>
   {
      services.AddHttpContextAccessor();
      services.AddAuditTrailFromAssemblies<TPermission>(assemblies, lifetime, filter, includeInternalTypes);
      services.AddScoped<IAuditTrailService<TPermission>, AuditTrailService<TPermission>>();
      services.AddScoped(typeof(IAuditTrailConsumer<TPermission>), typeof(TConsumer));

      services.AddSingleton<AuditTrailSaveInterceptor<TPermission>>();
      services.AddSingleton<AuditTrailDbTransactionInterceptor<TPermission>>();
      services.AddAuditTrailOptions(options);

      return services;
   }

   /// <summary>
   ///    Add AddAuditTrail which includes AuditTrail services
   /// </summary>
   /// <param name="TPermission">Type taht will be set in rule configuration for each entity rule</param>
   /// <param name="TConsumer">The implementation of IAuditTrailConsumer interface</param>
   /// <param name="TDecryption">The implementation of IAuditTrailDecryption interface</param>
   /// <param name="services">The collection of services</param>
   /// <param name="assemblies">The assemblies to scan</param>
   /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
   /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
   /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
   /// <returns>AssemblyScanner</returns>
   public static IServiceCollection AddAuditTrailWithDecryption<TPermission, TConsumer, TDecryption>(
      this IServiceCollection services,
      IEnumerable<Assembly> assemblies,
      Action<AuditTrailOptions>? options = null,
      ServiceLifetime lifetime = ServiceLifetime.Scoped,
      Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
      bool includeInternalTypes = false)
      where TConsumer : class, IAuditTrailConsumer<TPermission>
      where TDecryption : class, IAuditTrailDecryption
   {
      services.AddAuditTrail<TPermission, TConsumer>(assemblies, options, lifetime, filter, includeInternalTypes);
      services.AddScoped(typeof(IAuditTrailDecryption), typeof(TDecryption));

      return services;
   }

   /// <summary>
   ///    Add AddAuditTrail which includes AuditTrail services
   /// </summary>
   /// <param name="TPermission">Type taht will be set in rule configuration for each entity rule</param>
   /// <param name="TConsumer">The implementation of IAuditTrailConsumer interface</param>
   /// <param name="TInstance">The DbContext or any class</param>
   /// <param name="services">The collection of services</param>
   /// <param name="assemblies">The assemblies to scan</param>
   /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
   /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
   /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
   /// <returns>AssemblyScanner</returns>
   public static IServiceCollection AddAuditTrail<TPermission, TConsumer, TInstance>(this IServiceCollection services,
      IEnumerable<Assembly> assemblies,
      Action<AuditTrailOptions>? options = null,
      ServiceLifetime lifetime = ServiceLifetime.Scoped,
      Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
      bool includeInternalTypes = false)
      where TConsumer : class, IAuditTrailConsumer<TPermission, TInstance>
      where TInstance : class
   {
      services.AddHttpContextAccessor();
      services.AddAuditTrailFromAssemblies<TInstance>(assemblies, lifetime, filter, includeInternalTypes);
      services.AddScoped<IAuditTrailService<TPermission, TInstance>, AuditTrailService<TPermission, TInstance>>();
      services.AddScoped(typeof(IAuditTrailConsumer<TPermission, TInstance>), typeof(TConsumer));
      services.AddAuditTrailOptions(options);

      services.AddSingleton<AuditTrailSaveInterceptor<TPermission, TInstance>>();
      services.AddSingleton<AuditTrailDbTransactionInterceptor<TPermission, TInstance>>();

      return services;
   }

   /// <summary>
   ///    Add AddAuditTrail which includes AuditTrail services
   /// </summary>
   /// <param name="TPermission">Type taht will be set in rule configuration for each entity rule</param>
   /// <param name="TConsumer">The implementation of IAuditTrailConsumer interface</param>
   /// <param name="TInstance">The DbContext or any class</param>
   /// <param name="services">The collection of services</param>
   /// <param name="assemblies">The assemblies to scan</param>
   /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
   /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
   /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
   /// <returns>AssemblyScanner</returns>
   public static IServiceCollection AddAuditTrail<TPermission, TConsumer, TInstance>(this IServiceCollection services,
      Assembly assembly,
      Action<AuditTrailOptions>? options = null,
      ServiceLifetime lifetime = ServiceLifetime.Scoped,
      Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
      bool includeInternalTypes = false)
      where TConsumer : class, IAuditTrailConsumer<TPermission, TInstance>
      where TInstance : class
   {
      services.AddAuditTrail<TPermission, TConsumer, TInstance>([assembly],
         options,
         lifetime,
         filter,
         includeInternalTypes);

      return services;
   }

   /// <summary>
   ///    Add AddAuditTrail which includes AuditTrail services
   /// </summary>
   /// <param name="TPermission">Type taht will be set in rule configuration for each entity rule</param>
   /// <param name="TConsumer">The implementation of IAuditTrailConsumer interface</param>
   /// <param name="TInstance">The DbContext or any class</param>
   /// <param name="services">The collection of services</param>
   /// <param name="assemblies">The assemblies to scan</param>
   /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
   /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
   /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
   /// <returns>AssemblyScanner</returns>
   public static IServiceCollection AddAuditTrail<TPermission, TConsumer, TDecryption, TInstance>(
      this IServiceCollection services,
      IEnumerable<Assembly> assemblies,
      Action<AuditTrailOptions>? options = null,
      ServiceLifetime lifetime = ServiceLifetime.Scoped,
      Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
      bool includeInternalTypes = false)
      where TConsumer : class, IAuditTrailConsumer<TPermission, TInstance>
      where TDecryption : class, IAuditTrailDecryption
      where TInstance : class
   {
      services.AddAuditTrail<TPermission, TConsumer, TInstance>(assemblies,
         options,
         lifetime,
         filter,
         includeInternalTypes);
      services.AddScoped(typeof(IAuditTrailDecryption), typeof(TDecryption));
      services.AddAuditTrailOptions(options);

      return services;
   }

   private static IServiceCollection AddAuditTrailOptions(this IServiceCollection services,
      Action<AuditTrailOptions>? auditTrailOptions)
   {
      if (auditTrailOptions is null)
      {
         services.Configure<AuditTrailOptions>(options =>
         {
            options.AutoOpenTransaction = false;
         });
         return services;
      }

      services.Configure(auditTrailOptions);
      return services;
   }

   /// <summary>
   ///    Add AddAuditTrail which includes AuditTrail services
   /// </summary>
   /// <param name="TPermission">Type taht will be set in rule configuration for each entity rule</param>
   /// <param name="TConsumer">The implementation of IAuditTrailConsumer interface</param>
   /// <param name="TInstance">The DbContext or any class</param>
   /// <param name="services">The collection of services</param>
   /// <param name="assembly">The assembly to scan</param>
   /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
   /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
   /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
   /// <returns>AssemblyScanner</returns>
   public static IServiceCollection AddAuditTrail<TPermission, TConsumer, TDecryption, TInstance>(
      this IServiceCollection services,
      Assembly assembly,
      Action<AuditTrailOptions>? options = null,
      ServiceLifetime lifetime = ServiceLifetime.Scoped,
      Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
      bool includeInternalTypes = false)
      where TConsumer : class, IAuditTrailConsumer<TPermission, TInstance>
      where TDecryption : class, IAuditTrailDecryption
      where TInstance : class
   {
      services.AddAuditTrail<TPermission, TConsumer, TDecryption, TInstance>([assembly],
         options,
         lifetime,
         filter,
         includeInternalTypes);

      return services;
   }

   /// <summary>
   ///    Create AssemblyScanner which includes AuditTrail entity roles in specified assembly
   /// </summary>
   /// <param name="services">The collection of services</param>
   /// <param name="assembly">The assembly to scan</param>
   /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
   /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
   /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
   /// <returns>AssemblyScanner</returns>
   public static AssemblyScanner CreateAuditTrailAssemblyScannerFromAssembly(this IServiceCollection services,
      Assembly assembly,
      ServiceLifetime lifetime = ServiceLifetime.Scoped,
      Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
      bool includeInternalTypes = false)
   {
      var assemblyScanner = AssemblyScanner.FindTypeInAssembly(assembly, includeInternalTypes);
      assemblyScanner.ForEach(scanResult => services.AddScanResult(scanResult, lifetime, filter));

      return assemblyScanner;
   }

   /// <summary>
   ///    Adds all AuditTrail entity roles from specified assembly
   /// </summary>
   /// <param name="services">The collection of services</param>
   /// <param name="assemblies">The assemblies to scan</param>
   /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
   /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
   /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
   /// <returns></returns>
   public static IServiceCollection AddAuditTrailFromAssemblies<TInstance>(this IServiceCollection services,
      IEnumerable<Assembly> assemblies,
      ServiceLifetime lifetime = ServiceLifetime.Scoped,
      Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
      bool includeInternalTypes = false)
   {
      ArgumentNullException.ThrowIfNull(assemblies);

      List<AssemblyScanner> scanners = [];
      foreach (var assembly in assemblies)
      {
         var assemblyScanner = AssemblyScanner.FindTypeInAssembly(assembly, includeInternalTypes);
         assemblyScanner.ForEach(scanResult => services.AddScanResult(scanResult, lifetime, filter));
         scanners.Add(assemblyScanner);
      }

      services.AddSingleton<IAuditTrailAssemblyProvider<TInstance>>(
         new AuditTrailAssemblyProvider<TInstance>(scanners));

      return services;
   }

   public static DbContextOptionsBuilder UseAuditTrail<TPermission>(this DbContextOptionsBuilder optionsBuilder,
      IServiceProvider serviceProvider)
   {
      var auditSaveInterceptor = serviceProvider.GetRequiredService<AuditTrailSaveInterceptor<TPermission>>();
      var auditDbTransactionInterceptor =
         serviceProvider.GetRequiredService<AuditTrailDbTransactionInterceptor<TPermission>>();

      optionsBuilder.AddInterceptors(auditSaveInterceptor);
      optionsBuilder.AddInterceptors(auditDbTransactionInterceptor);

      return optionsBuilder;
   }

   public static DbContextOptionsBuilder UseAuditTrail<TPermission, TInstance>(
      this DbContextOptionsBuilder optionsBuilder,
      IServiceProvider serviceProvider)
      where TInstance : class
   {
      var auditSaveInterceptor =
         serviceProvider.GetRequiredService<AuditTrailSaveInterceptor<TPermission, TInstance>>();
      var auditDbTransactionInterceptor =
         serviceProvider.GetRequiredService<AuditTrailDbTransactionInterceptor<TPermission, TInstance>>();

      optionsBuilder.AddInterceptors(auditSaveInterceptor);
      optionsBuilder.AddInterceptors(auditDbTransactionInterceptor);

      return optionsBuilder;
   }

   /// <summary>
   ///    Adds all AuditTrail entity roles from specified assembly
   /// </summary>
   /// <param name="services">The collection of services</param>
   /// <param name="assembly">The assembly to scan</param>
   /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
   /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
   /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
   /// <returns></returns>
   public static IServiceCollection AddAuditTrailFromAssembly<TInstance>(this IServiceCollection services,
      Assembly assembly,
      ServiceLifetime lifetime = ServiceLifetime.Scoped,
      Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
      bool includeInternalTypes = false)
   {
      ArgumentNullException.ThrowIfNull(assembly);

      AssemblyScanner
         .FindTypeInAssembly(assembly, includeInternalTypes)
         .AddAssemblyToProvider<TInstance>(services)
         .ForEach(scanResult => services.AddScanResult(scanResult, lifetime, filter));

      return services;
   }

   /// <summary>
   ///    Helper method to register a AuditTrail from an AssemblyScanner result
   /// </summary>
   /// <param name="services">The collection of services</param>
   /// <param name="scanResult">The scan result</param>
   /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web applications)</param>
   /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
   /// <returns></returns>
   private static IServiceCollection AddScanResult(this IServiceCollection services,
      AssemblyScanner.AssemblyScanResult scanResult,
      ServiceLifetime lifetime,
      Func<AssemblyScanner.AssemblyScanResult, bool> filter)
   {
      var shouldRegister = filter?.Invoke(scanResult) ?? true;
      if (shouldRegister)
      {
         //Register as interface
         services.TryAddEnumerable(
            new ServiceDescriptor(
               scanResult.InterfaceType,
               scanResult.RuleType,
               lifetime));

         //Register as self
         services.TryAdd(
            new ServiceDescriptor(
               scanResult.RuleType,
               scanResult.RuleType,
               lifetime));
      }

      return services;
   }

   private static AssemblyScanner AddAssemblyToProvider<TInstance>(this AssemblyScanner assemblyScans,
      IServiceCollection services)
   {
      services.AddSingleton<IAuditTrailAssemblyProvider<TInstance>>(
         new AuditTrailAssemblyProvider<TInstance>([assemblyScans]));
      return assemblyScans;
   }
}