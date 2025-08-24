using Makabaka.Events;
using Microsoft.Extensions.Logging;
using IMessageHandler = TreePassBot.Handlers.MessageHandler.Interfaces.IMessageHandler;

namespace TreePassBot.Handlers.MessageHandler.Handlers;

public class EndPointMessageHandler(ILogger<EndPointMessageHandler> logger) : IMessageHandler
{
    /// <inheritdoc />
    public Task InvokeAsync(GroupMessageEventArgs e, HandlerLinkNode? next)
    {
#if DEBUG
        logger.LogInformation("Reached endpoint: {Message}", e.Message.ToString());
#endif
        return Task.CompletedTask;
    }
}