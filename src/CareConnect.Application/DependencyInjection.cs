using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CareConnect.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);
        return services;
    }
}
