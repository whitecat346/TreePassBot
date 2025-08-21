using Makabaka.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Exceptions;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers;

public class GroupRequestEventHandler(
    IUserService userService,
    IOptions<BotConfig> config,
    IMessageService messageService,
    ILogger<GroupRequestEventHandler> logger)
{
    private readonly BotConfig _config = config.Value;

    public async Task HandleAddRequest(GroupAddRequestEventArgs e)
    {
        if (_config.MainGroupIds.Contains(e.GroupId))
        {
            await MainGroupHandlerAsync(e).ConfigureAwait(false);
        }

        if (_config.AuditGroupId == e.GroupId)
        {
            await AuditGroupHandlerAsync(e).ConfigureAwait(false);
        }
    }

    private async Task AuditGroupHandlerAsync(GroupAddRequestEventArgs e)
    {
        if (await userService.IsInBlackList(e.UserId).ConfigureAwait(false))
        {
            await e.RejectAsync("您在黑名单中，无法加入该群组！").ConfigureAwait(false);
            logger.LogInformation("User {qqId} is in the blacklist.", e.UserId);
            return;
        }

        await e.AcceptAsync().ConfigureAwait(false);
        logger.LogInformation("New user {qqId} joined audit group {groupId}", e.UserId, e.GroupId);
    }

    private async Task MainGroupHandlerAsync(GroupAddRequestEventArgs e)
    {
        var userRequestComment = e.Comment.Trim([' ', '\n']);

        try
        {
            var (rightPasscode, expiredPasscode) =
                await userService.ValidateJoinRequestAsync(e.UserId, userRequestComment).ConfigureAwait(false);
            if (rightPasscode)
            {
                await e.AcceptAsync().ConfigureAwait(false);
                await userService.DeleteUserAsync(e.UserId).ConfigureAwait(false);
                logger.LogInformation("User {qqId} passed audit.", e.UserId);

                await messageService.KickGroupMemberAsync(_config.AuditGroupId, e.UserId).ConfigureAwait(false);
            }
            else if (expiredPasscode)
            {
                await e.RejectAsync("验证码已过期，请重新申请审核！").ConfigureAwait(false);
                logger.LogInformation("User {qqId} 's passcode was expired.", e.UserId);
            }
            else
            {
                await e.RejectAsync("验证码不正确！").ConfigureAwait(false);
                logger.LogInformation("User {qqId} was denied by wrong passcode.", e.UserId);
            }
        }
        catch (UserNotFoundException ex)
        {
            logger.LogInformation(ex.Message);
            await e.RejectAsync("您不在审核名单中！").ConfigureAwait(false);
        }
    }
}