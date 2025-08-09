using Makabaka.Events;
using Microsoft.Extensions.Options;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers;

public class GroupRequestEventHandler(
    IUserService userService,
    IOptions<BotConfig> config)
{
    public async Task HandleAddRequest(object sender, GroupAddRequestEventArgs e)
    {
        if (e.GroupId != config.Value.AuditGroupId)
        {
            return;
        }

        var rightPasscode = await userService.ValidateJoinRequestAsync(e.UserId, e.Comment);
        if (rightPasscode)
        {
            await e.AcceptAsync();
        }
        else
        {
            await e.RejectAsync("验证码不正确！");
        }
    }
}