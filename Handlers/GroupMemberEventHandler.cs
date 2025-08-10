using Makabaka.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers;

public class GroupMemberEventHandler(
    IUserService userService,
    ILogger<GroupMemberEventHandler> logger,
    IOptions<BotConfig> config)
{
    private readonly BotConfig _config = config.Value;

    public async Task HandleGroupMemberIncrease(GroupMemberIncreaseEventArgs e)
    {
        if (e.GroupId != _config.AuditGroupId)
        {
            return;
        }

        logger.LogInformation("New member {UserId} joined group {GroupId}.", e.UserId, e.GroupId);
        await userService.AddPendingUserAsync(e.UserId);
    }

    public async Task HandleGroupMemberDecrease(GroupMemberDecreaseEventArgs e)
    {
        if (e.GroupId != _config.AuditGroupId)
        {
            return;
        }

        logger.LogInformation("Remove {UserId} from {groupId}", e.UserId, e.GroupId);
        await userService.DeleteUserUserAsync(e.UserId);
    }
}