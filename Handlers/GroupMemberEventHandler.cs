using Makabaka.Events;
using Makabaka.Messages;
using Makabaka.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers;

public class GroupMemberEventHandler(
    IUserService userService,
    ILogger<GroupMemberEventHandler> logger,
    IMessageService messageService,
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

        await messageService.SendGroupMessageAsync(e.GroupId,
        [
            new AtSegment(e.UserId),
            new TextSegment("欢迎来到审核群，请填写群公告中的问卷进行审核：\n"),
            new TextSegment("建议使用浏览器访问，而不是在QQ中打开。")
        ]);
    }

    public async Task HandleGroupMemberDecrease(GroupMemberDecreaseEventArgs e)
    {
        if (e.GroupId != _config.AuditGroupId)
        {
            var userInfo = await messageService.GetGroupMemberInfo(e.GroupId, e.UserId);

            if (userInfo?.Role is GroupRoleType.Admin or GroupRoleType.Owner)
            {
                AddBanedMember(e);
            }

            return;
        }

        logger.LogInformation("Remove {UserId} from {GroupId}", e.UserId, e.GroupId);
        await userService.DeleteUserAsync(e.UserId);
    }

    private void AddBanedMember(GroupMemberDecreaseEventArgs e)
    {
        userService.AddToBlackList(e.UserId);
    }
}