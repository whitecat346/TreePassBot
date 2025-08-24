using System.Reflection;
using Microsoft.Extensions.Logging;

namespace TreePassBot.Handlers.MessageHandler;

public class HandlerLinkNodeFactory(
    IServiceProvider serviceProvider,
    ILogger<HandlerLinkNode> logger)
{
    public HandlerLinkNode Create(Type moduleTypem, MethodInfo method, HandlerLinkNode? nextNode)
    {
        return new HandlerLinkNode(serviceProvider, logger)
        {
            NextMethod = method,
            NextModuleType = moduleTypem,
            NextNode = nextNode
        };
    }
}