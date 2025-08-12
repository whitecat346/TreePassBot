using Makabaka.Events;
using Makabaka.Messages;
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

        var queUrlIndex = Random.Shared.Next(0, 3);
        var queUrl = _config.QuestionnaireLinks[queUrlIndex];

        await messageService.SendGroupMessageAsync(e.GroupId,
        [
            new AtSegment(e.UserId),
            new TextSegment("欢迎来到审核群，请填写下面的问卷进行审核：\n"),
            new TextSegment(queUrl),
            new TextSegment("\n建议使用浏览器访问，而不是在QQ中打开。")
        ]);
    }

    public async Task HandleGroupMemberDecrease(GroupMemberDecreaseEventArgs e)
    {
        if (e.GroupId != _config.AuditGroupId)
        {
            return;
        }

        logger.LogInformation("Remove {UserId} from {groupId}", e.UserId, e.GroupId);
        await userService.DeleteUserAsync(e.UserId);
    }
}