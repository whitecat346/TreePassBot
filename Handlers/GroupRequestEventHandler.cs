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
    public async Task HandleAddRequest(GroupAddRequestEventArgs e)
    {
        if (!config.Value.MainGroupId.Contains(e.GroupId))
        {
            return;
        }

        try
        {
            var rightPasscode = await userService.ValidateJoinRequestAsync(e.UserId, e.Comment);
            if (rightPasscode)
            {
                await e.AcceptAsync();
                await userService.DeleteUserUserAsync(e.UserId);
                logger.LogInformation("User {qqId} passed audit.", e.UserId);

                await messageService.SendPrivateMessageAsync(e.UserId, [new TextSegment("您的申请已通过！")]);
                await QqBotService.MakabakaApp.BotContext.KickGroupMemberAsync(config.Value.AuditGroupId, e.UserId);
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