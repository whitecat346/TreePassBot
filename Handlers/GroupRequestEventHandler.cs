using Makabaka.Events;
using Makabaka.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Models;
using TreePassBot.Services;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers;

public class GroupRequestEventHandler(
    IUserService userService,
    IMessageService messageService,
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
    }

    private async Task MainGroupHandlerAsync(GroupAddRequestEventArgs e)
    {
        try
        {
            var rightPasscode = await userService.ValidateJoinRequestAsync(e.UserId, e.Comment);
            if (rightPasscode)
            {
                await e.AcceptAsync();
                await userService.DeleteUserAsync(e.UserId);
                logger.LogInformation("User {qqId} passed audit.", e.UserId);

                await QqBotService.MakabakaApp.BotContext.KickGroupMemberAsync(config.Value.AuditGroupId, e.UserId);
            }
            else
            {
                await e.RejectAsync("验证码不正确！可能已经过期，请私信机器人刷新");
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