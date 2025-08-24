using Makabaka.Events;
using TreePassBot.Handlers.Commands;
using IMessageHandler = TreePassBot.Handlers.MessageHandler.Interfaces.IMessageHandler;

namespace TreePassBot.Handlers.MessageHandler.Handlers;

public class AdminCommandMessageHandler(CommandDispatcher commandDispatcher) : IMessageHandler
{
    /// <inheritdoc />
    public async Task InvokeAsync(GroupMessageEventArgs e, HandlerLinkNode? next)
    {
        var success = await commandDispatcher.ExecteAsync(e);
        if (!success)
        {
            next?.Next(e);
        }
    }
}