using Makabaka.Events;

namespace TreePassBot.Handlers.MessageHandler.Interfaces;

public interface IMessageHandler
{
    Task InvokeAsync(GroupMessageEventArgs e, HandlerLinkNode? next);
}