using Makabaka.Events;
using Microsoft.Extensions.Logging;
using TreePassBot.Handlers.MessageHandler;

namespace TreePassBot.Handlers;

public class GroupMessageEventHandler(
    MessageHandlerDispatcher dispatcher,
    ILogger<GroupMessageEventHandler> logger)
{
    public async Task HandleGroupMessage(GroupMessageEventArgs e)
    {
#if DEBUG
        logger.LogInformation("Handler Message: {Message}", e.Message.ToString());
#endif
        await dispatcher.HandleMessage(e);
    }
}