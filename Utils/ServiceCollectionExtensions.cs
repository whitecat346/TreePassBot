using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TreePassBot.Handlers.AdminCommands.Data;

namespace TreePassBot.Utils;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommandModules(this IServiceCollection service, Assembly assembly)
    {
        var commandModuleTypes = assembly.GetTypes()
                                         .Where(t => t is { IsClass: true, IsAbstract: false } &&
                                                     t.GetMethods().Any(m => m.IsDefined(typeof(BotCommand))));
        foreach (var type in commandModuleTypes)
        {
            service.AddScoped(type);
        }

        return service;
    }
}