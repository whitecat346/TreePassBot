using Makabaka.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using IMessageHandler = TreePassBot.Handlers.MessageHandler.Interfaces.IMessageHandler;

namespace TreePassBot.Handlers.MessageHandler;

public class MessageHandlerDispatcher
{
    private readonly ILogger<MessageHandlerDispatcher> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<HandlerLinkNode> _nodes = [];

    public MessageHandlerDispatcher(ILogger<MessageHandlerDispatcher> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

#if DEBUG
        _ = UseHandler<EndpointMessageHandler>();
#endif
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public MessageHandlerDispatcher UseHandler<THandler>() where THandler : IMessageHandler
    {
        return UseHandler(typeof(THandler));
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public MessageHandlerDispatcher UseHandler(Type handlerType)
    {
        if (handlerType is { IsClass: false, IsAbstract: true })
        {
            return this;
        }

        if (!typeof(IMessageHandler).IsAssignableFrom(handlerType))
        {
            _logger.LogWarning("Class {Name} not implement interface 'IMessageHandler', skipped", handlerType.Name);
            return this;
        }

        var methods = handlerType.GetMethods()
                                 .Where(m => m.IsPublic);
        MethodInfo? targetMethod = null;
        foreach (var method in methods)
        {
            if (targetMethod is not null)
            {
                throw new InvalidOperationException("Method 'InvokeAsync' has already exist.");
            }

            if (!string.Equals(method.Name, "InvokeAsync", StringComparison.Ordinal)) continue;

            var paramaters = method.GetParameters();
            if (paramaters[0].ParameterType != typeof(GroupMessageEventArgs) &&
                paramaters[1].ParameterType != typeof(HandlerLinkNode))
            {
                _logger.LogWarning("Finded 'InvokeAsync' not matched rules because of paramaters");
                continue;
            }

            targetMethod = method;
            break;
        }

        if (targetMethod is null)
        {
            throw new InvalidOperationException($"'InvokeAsync' method not found in {handlerType.Name}.");
        }

        if (_serviceProvider.GetRequiredService(typeof(HandlerLinkNodeFactory)) is not HandlerLinkNodeFactory factory)
        {
            throw new InvalidOperationException("'HandlerLinkNodeFactory' not exist in DI.");
        }

        var nextNode = _nodes.LastOrDefault();
        var node = factory.Create(handlerType, targetMethod, nextNode);
        _nodes.Add(node);

#if DEBUG
        _logger.LogInformation("Add handler: {Name}", handlerType.Name);
#endif

        return this;
    }

    public Task HandleMessage(GroupMessageEventArgs args)
    {
        var node = _nodes.LastOrDefault();
        if (node is null)
        {
            throw new InvalidOperationException("No any node exist.");
        }

        return node.Next(args);
    }
}