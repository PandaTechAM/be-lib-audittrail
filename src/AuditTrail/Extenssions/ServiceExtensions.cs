﻿namespace AuditTrail.Extenssions;

using AuditTrail.Abstraction;
using AuditTrail.Fluent;
using AuditTrail.Fluent.Abstraction;
using AuditTrail.Interceptors;
using AuditTrail.Services;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security;

public static class ServiceExtensions
{
    /// <summary>
    /// Add AddAuditTrail which includes AuditTrail services
    /// </summary>
    /// <param name="TPermission">Type taht will be set in rule configuration for each entity rule</param>
    /// <param name="TConsumer">The implementation of IAuditTrailConsumer interface</param>
    /// <param name="services">The collection of services</param>
    /// <param name="assemblies">The assembly to scan</param>
    /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
    /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
    /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
    /// <returns>AssemblyScanner</returns>
    public static IServiceCollection AddAuditTrail<TPermission, TConsumer>(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
        bool includeInternalTypes = false)
        where TConsumer : class, IAuditTrailConsumer<TPermission>
    {
        services.AddAuditTrailFromAssemblies(assemblies, lifetime, filter, includeInternalTypes);
        services.AddScoped<IAuditTrailService<TPermission>, AuditTrailService<TPermission>>();
        services.AddScoped(typeof(IAuditTrailConsumer<TPermission>), typeof(TConsumer));

        services.AddSingleton<AuditTrailSaveInterceptor<TPermission?>>();
        services.AddSingleton<AuditTrailDbTransactionInterceptor<TPermission?>>();

        return services;
    }

    /// <summary>
    /// Add AddAuditTrail which includes AuditTrail services
    /// </summary>
    /// <param name="TPermission">Type taht will be set in rule configuration for each entity rule</param>
    /// <param name="TConsumer">The implementation of IAuditTrailConsumer interface</param>
    /// <param name="TDecryption">The implementation of IAuditTrailDecryption interface</param>
    /// <param name="services">The collection of services</param>
    /// <param name="assemblies">The assembly to scan</param>
    /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
    /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
    /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
    /// <returns>AssemblyScanner</returns>
    public static IServiceCollection AddAuditTrail<TPermission, TConsumer, TDecryption>(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!,
        bool includeInternalTypes = false)
        where TConsumer : class, IAuditTrailConsumer<TPermission>
        where TDecryption : class, IAuditTrailDecryption
    {
        services.AddAuditTrail<TPermission, TConsumer>(assemblies, lifetime, filter, includeInternalTypes);
        services.AddScoped(typeof(IAuditTrailDecryption), typeof(TDecryption));

        return services;
    }

    /// <summary>
    /// Create AssemblyScanner which includes AuditTrail entity roles in specified assembly
    /// </summary>
    /// <param name="services">The collection of services</param>
    /// <param name="assembly">The assembly to scan</param>
    /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
    /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
    /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
    /// <returns>AssemblyScanner</returns>
    public static AssemblyScanner CreateAuditTrailAssemblyScannerFromAssembly(this IServiceCollection services, Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped, Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!, bool includeInternalTypes = false)
    {
        var assemblyScanner = AssemblyScanner.FindTypeInAssembly(assembly, includeInternalTypes);
        assemblyScanner.ForEach(scanResult => services.AddScanResult(scanResult, lifetime, filter));

        return assemblyScanner;
    }

    /// <summary>
    /// Adds all AuditTrail entity roles from specified assembly
    /// </summary>
    /// <param name="services">The collection of services</param>
    /// <param name="assemblies">The assemblies to scan</param>
    /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
    /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
    /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
    /// <returns></returns>
	public static IServiceCollection AddAuditTrailFromAssemblies(this IServiceCollection services, IEnumerable<Assembly> assemblies, ServiceLifetime lifetime = ServiceLifetime.Scoped, Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!, bool includeInternalTypes = false)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        List<AssemblyScanner> scanners = [];
        foreach (var assembly in assemblies)
        {
            var assemblyScanner = AssemblyScanner.FindTypeInAssembly(assembly, includeInternalTypes);
            assemblyScanner.ForEach(scanResult => services.AddScanResult(scanResult, lifetime, filter));
            scanners.Add(assemblyScanner);
        }

        services.AddSingleton<IAuditTrailAssemblyProvider>(new AuditTrailAssemblyProvider(scanners));

        return services;
    }

    public static DbContextOptionsBuilder UseAuditTrail<TPermission>(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
    {
        var auditSaveInterceptor = serviceProvider.GetRequiredService<AuditTrailSaveInterceptor<TPermission>>();
        var auditDbTransactionInterceptor = serviceProvider.GetRequiredService<AuditTrailDbTransactionInterceptor<TPermission>>();

        optionsBuilder.AddInterceptors(auditSaveInterceptor);
        optionsBuilder.AddInterceptors(auditDbTransactionInterceptor);

        return optionsBuilder;
    }

    /// <summary>
    /// Adds all AuditTrail entity roles from specified assembly
    /// </summary>
    /// <param name="services">The collection of services</param>
    /// <param name="assembly">The assembly to scan</param>
    /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
    /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
    /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
    /// <returns></returns>
    public static IServiceCollection AddAuditTrailFromAssembly(this IServiceCollection services, Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped, Func<AssemblyScanner.AssemblyScanResult, bool> filter = null!, bool includeInternalTypes = false)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        AssemblyScanner
            .FindTypeInAssembly(assembly, includeInternalTypes)
            .AddAssemblyToProvider(services)
            .ForEach(scanResult => services.AddScanResult(scanResult, lifetime, filter));

        return services;
    }

    /// <summary>
    /// Helper method to register a AuditTrail from an AssemblyScanner result
    /// </summary>
    /// <param name="services">The collection of services</param>
    /// <param name="scanResult">The scan result</param>
    /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web applications)</param>
    /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
    /// <returns></returns>
    private static IServiceCollection AddScanResult(this IServiceCollection services, AssemblyScanner.AssemblyScanResult scanResult, ServiceLifetime lifetime, Func<AssemblyScanner.AssemblyScanResult, bool> filter)
    {
        bool shouldRegister = filter?.Invoke(scanResult) ?? true;
        if (shouldRegister)
        {
            //Register as interface
            services.TryAddEnumerable(
                new ServiceDescriptor(
                    serviceType: scanResult.InterfaceType,
                    implementationType: scanResult.RuleType,
                    lifetime: lifetime));

            //Register as self
            services.TryAdd(
                new ServiceDescriptor(
                    serviceType: scanResult.RuleType,
                    implementationType: scanResult.RuleType,
                    lifetime: lifetime));
        }

        return services;
    }

    private static AssemblyScanner AddAssemblyToProvider(this AssemblyScanner assemblyScans, IServiceCollection services)
    {
        services.AddSingleton<IAuditTrailAssemblyProvider>(new AuditTrailAssemblyProvider([assemblyScans]));
        return assemblyScans;
    }
}
