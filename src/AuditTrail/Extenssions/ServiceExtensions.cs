namespace AuditTrail.Extenssions;

using AuditTrail.Fluent;
using AuditTrail.Fluent.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;

public static class ServiceExtensions
{
    public static AssemblyScanner AddAssemblyToProvider(this AssemblyScanner assemblyScans, IServiceCollection services)
    {
        services.AddSingleton<IAuditTrailAssemblyProvider>(new AuditTrailAssemblyProvider(assemblyScans));
        return assemblyScans;
    }

    /// <summary>
    /// Adds all AuditTrail in specified assembly
    /// </summary>
    /// <param name="services">The collection of services</param>
    /// <param name="assembly">The assembly to scan</param>
    /// <param name="lifetime">The lifetime of the AuditTrail. The default is scoped (per-request in web application)</param>
    /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
    /// <param name="includeInternalTypes">Include internal AuditTrail. The default is false.</param>
    /// <returns></returns>
    public static IServiceCollection AddAuditTrailFromAssembly(this IServiceCollection services, Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped, Func<AssemblyScanner.AssemblyScanResult, bool> filter = null, bool includeInternalTypes = false)
    {
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
}
