using Makabaka;
using Microsoft.Extensions.Logging;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Services;

public class MessageService(ILogger<MessageService> logger) : IMessageService
{
    public async Task SendGroupMessageAsync(ulong groupId, Makabaka.Messages.Message msg)
    {
        logger.LogInformation($"Send group message to {groupId}. \nContent: {msg}");
        await QqBotService.MakabakaApp.BotContext.SendGroupMessageAsync(groupId, msg);
    }

    public async Task SendPrivateMessageAsync(ulong userId, Makabaka.Messages.Message msg)
    {
        logger.LogInformation($"Send private message to {userId}. \nContent: {msg}");
        await QqBotService.MakabakaApp.BotContext.SendPrivateMessageAsync(userId, msg);
    }
}