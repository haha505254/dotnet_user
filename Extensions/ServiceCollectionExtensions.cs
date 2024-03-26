using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

public static class ServiceCollectionExtensions
{
    public static void AddServicesInAssembly(this IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                Service = t.GetInterface($"I{t.Name}"),
                Implementation = t
            })
            .Where(t => t.Service != null);

        foreach (var type in types)
        {
            if (type.Service?.Name?.EndsWith("Repository") ?? false)
            {
                services.AddScoped(type.Service, type.Implementation);
            }
            else if (type.Service?.Name?.EndsWith("Service") ?? false)
            {
                services.AddScoped(type.Service, type.Implementation);
            }
        }
    }
}