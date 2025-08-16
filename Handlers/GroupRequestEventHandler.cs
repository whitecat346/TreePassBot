using Makabaka.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Models;
using TreePassBot.Services;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers;

public class GroupRequestEventHandler(
    IUserService userService,
    IOptions<BotConfig> config,
    ILogger<GroupRequestEventHandler> logger)
{
    private readonly BotConfig _config = config.Value;

    public async Task HandleAddRequest(GroupAddRequestEventArgs e)
    {
        if (_config.MainGroupIds.Contains(e.GroupId))
        {
            await MainGroupHandlerAsync(e);
        }

        if (_config.AuditGroupId == e.GroupId)
        {
            await AuditGroupHandlerAsync(e);
        }
    }

    private async Task AuditGroupHandlerAsync(GroupAddRequestEventArgs e)
    {
        await e.AcceptAsync();
        logger.LogInformation("New user {qqId} joined audit group {groupId}", e.UserId, e.GroupId);
    }

    private async Task MainGroupHandlerAsync(GroupAddRequestEventArgs e)
    {
        try
        {
            var (rightPasscode, expriedPasscode) = await userService.ValidateJoinRequestAsync(e.UserId, e.Comment);
            if (rightPasscode)
            {
                await e.AcceptAsync();
                await userService.DeleteUserAsync(e.UserId);
                logger.LogInformation("User {qqId} passed audit.", e.UserId);

                await QqBotService.MakabakaApp.BotContext.KickGroupMemberAsync(config.Value.AuditGroupId, e.UserId);
            }
            else if (expriedPasscode)
            {
                await e.RejectAsync("验证码以过期，请重新申请审核！");
                logger.LogInformation("User {qqId} 's passcode was expried.", e.UserId);
            }
            else
            {
                await e.RejectAsync("验证码不正确！");
                logger.LogInformation("User {qqId} was denied by wrong passcode.", e.UserId);
            }
        }
        catch (ArgumentNullException)
        {
            logger.LogInformation("User {qqId} not found.", e.UserId);
            await e.RejectAsync("您不在审核名单中！");
        }
    }
}