using System.Reflection;
using Makabaka.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TreePassBot.Handlers.MessageHandler;

public class HandlerLinkNode(
    IServiceProvider serviceProvider,
    ILogger<HandlerLinkNode> logger)
{
    public required Type NextModuleType { get; init; }
    public required MethodInfo NextMethod { get; init; }

    public required HandlerLinkNode? NextNode { get; init; }

    public Task Next(GroupMessageEventArgs args)
    {
#if DEBUG
        logger.LogInformation("In handler: {Name}", NextModuleType.Name);
#endif

        try
        {
            using var scoop = serviceProvider.CreateScope();
            var moduleInstance = scoop.ServiceProvider.GetRequiredService(NextModuleType);

            if (NextMethod.Invoke(moduleInstance, [args, NextNode]) is Task methodTask) return methodTask;

            logger.LogError("Failed to create message handler method at {HandlerName}", NextModuleType.Name);
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            logger.LogError("Occurred an exception when handle message at {HandlerName}: {Exception}",
                            NextModuleType.Name, e);
            return Task.CompletedTask;
        }
    }
}