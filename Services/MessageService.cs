using Makabaka.Exceptions;
using Makabaka.Models;
using Microsoft.Extensions.Logging;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Services;

public class MessageService(ILogger<MessageService> logger) : IMessageService
{
    public async Task SendGroupMessageAsync(ulong groupId, Makabaka.Messages.Message msg)
    {
        logger.LogInformation("Send group message to {GroupId}. \nContent: {Msg}", groupId, msg);
        await QqBotService.MakabakaApp.BotContext.SendGroupMessageAsync(groupId, msg);
    }

    public async Task SendPrivateMessageAsync(ulong userId, Makabaka.Messages.Message msg)
    {
        logger.LogInformation("Send private message to {UserId}. \nContent: {Msg}", userId, msg);
        await QqBotService.MakabakaApp.BotContext.SendPrivateMessageAsync(userId, msg);
    }

    public async Task<GroupMemberInfo?> GetGroupMemberInfo(ulong groupId, ulong userId)
    {
        logger.LogInformation("Try to get group {GroupId} member {MemberId} info.", groupId, userId);
        var response = await QqBotService.MakabakaApp.BotContext.GetGroupMemberInfoAsync(groupId, userId);
        try
        {
            response.EnsureSuccess();
            return response.Result;
        }
        catch (APIResponseDataNullException)
        {
            return null;
        }
    }

    public async Task<GroupMemberInfo[]?> GetGroupMemberList(ulong groupId)
    {
        logger.LogInformation("Try to get group {GroupId} member list.", groupId);
        var response = await QqBotService.MakabakaApp.BotContext.GetGroupMemberListAsync(groupId);
        try
        {
            response.EnsureSuccess();
            return response.Result;
        }
        catch (APIResponseDataNullException)
        {
            return null;
        }
    }
}