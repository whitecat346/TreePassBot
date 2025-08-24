using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TreePassBot.Handlers.Commands.Data;
using TreePassBot.Handlers.MessageHandler.Interfaces;

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

    public static IServiceCollection AddMessageModules(this IServiceCollection service, Assembly assembly)
    {
        var handlerModuleTypes = assembly.GetTypes()
                                         .Where(t => typeof(IMessageHandler).IsAssignableFrom(t)
                                                  && t is { IsInterface: false, IsAbstract: false });
        foreach (var type in handlerModuleTypes)
        {
            service.AddScoped(type);
        }

        return service;
    }
}