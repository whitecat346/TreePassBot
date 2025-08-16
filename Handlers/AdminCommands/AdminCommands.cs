using System.Text;
using Makabaka.Events;
using Makabaka.Messages;
using TreePassBot.Data;
using TreePassBot.Handlers.AdminCommands.Permission;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers.AdminCommands;

public class AdminCommands(
    JsonDataStore dataStore,
    IUserService userService)
{
    [BotCommand("rand", Description = "生成一个随机验证码", Usage = ".rand")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> RandomGeneration(GroupMessageEventArgs e)
    {
        var passcode = new string(Enumerable.Range(0, 10)
                                            .Select(_ => (char)Random.Shared.Next('0', '9' + 1))
                                            .ToArray());

        var isUnique = !dataStore.PasscodeExists(passcode);

        await e.ReplyAsync([new TextSegment($"生成测试验证码: {passcode}\n在当前数据中是否唯一: {isUnique}")]);

        return true;
    }

    [BotCommand("check", Description = "查询用户状态", Usage = ".check [QQ号]")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> CheckUser(GroupMessageEventArgs e)
    {
        var spilied = e.Message.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (!ulong.TryParse(spilied[1], out var qqToCheck))
        {
            return false;
        }

        var user = await userService.GetPendingUserAsync(qqToCheck);
        if (user == null)
        {
            var isInBlackList = await userService.IsInBlackList(qqToCheck);
            if (isInBlackList)
            {
                await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"用户 {qqToCheck} 位于黑名单中。")]);
                return true;
            }

            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"用户 {qqToCheck} 未在数据存储中找到。")]);
            return true;
        }

        var userInfo = new StringBuilder($"查询用户: {user.QqId}\n" +
                                         $"状态: {user.Status}\n" +
                                         $"验证码: {(string.IsNullOrEmpty(user.Passcode) ? "无" : user.Passcode)}\n" +
                                         $"创建时间: {user.CreatedAt:yyyy-MM-dd HH:mm:ss} (UTC)\n" +
                                         $"更新时间: {user.UpdatedAt:yyyy-MM-dd HH:mm:ss} (UTC)");

        if (user.ExpriedAt != null)
        {
            userInfo.Append($"\n过期时间: {user.ExpriedAt:yyyy-MM-dd HH:mm:ss} (UTC)");
        }

        await e.ReplyAsync([new TextSegment(userInfo.ToString())]);
        return true;
    }

    [BotCommand("add-user", Description = "添加用户到待审核列表", Usage = ".add-user [QQ号]")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> AddUser(GroupMessageEventArgs e)
    {
        var spilied = e.Message.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (spilied.Length < 2 || !ulong.TryParse(spilied[1], out var qqToAdd))
        {
            return false;
        }

        var success = await userService.AddPendingUserAsync(qqToAdd);
        if (success)
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"成功将用户 {qqToAdd} 添加到待审核列表。")]);
        }
        else
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"无法添加用户 {qqToAdd}，可能已存在或发生错误。")]);
        }


        return true;
    }

    [BotCommand("reset", Description = "重置用户状态", Usage = ".reset [qq号]")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> ResetUser(GroupMessageEventArgs e)
    {
        var spilied = e.Message.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (spilied.Length < 2 || !ulong.TryParse(spilied[1], out var qqToReset))
        {
            return false;
        }

        await userService.DeleteUserAsync(qqToReset);
        var success = await userService.AddPendingUserAsync(qqToReset);

        if (success)
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"成功将用户 {qqToReset} 重置为待审核状态。")]);
        }
        else
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"无法重置用户 {qqToReset}，可能已存在或发生错误。")]);
        }

        return true;
    }

    [BotCommand("audit-help", Description = "查看审核相关命令", Usage = ".audit-help")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> AuditHelp(GroupMessageEventArgs e)
    {
        const string auditHelpText = "审核员命令:\n" +
                                     "使用 @+QQ号 pass - 通过指定用户的审核\n" +
                                     "使用 @+QQ号 deny - 拒绝指定用户的审核";
        await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment(auditHelpText)]);
        return true;
    }

    [BotCommand("add-black", Description = "将指定的用用户添加进黑名单", Usage = ".add-black [QQ号]")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> AddBlackUser(GroupMessageEventArgs e)
    {
        var spilied = e.Message.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (spilied.Length < 2 || !ulong.TryParse(spilied[1], out var qqToAdd))
        {
            return false;
        }

        await userService.AddToBlackList(qqToAdd);

        await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"成功将用户 {qqToAdd} 添加到黑名单。")]);

        return true;
    }

    [BotCommand("rm-black", Description = "将指定的用户从黑名单中移除", Usage = ".rm-black [QQ号]")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> RemoveBlackUser(GroupMessageEventArgs e)
    {
        var spilied = e.Message.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (spilied.Length < 2 || !ulong.TryParse(spilied[1], out var qqToRemove))
        {
            return false;
        }

        await userService.RemoveFromBlackList(qqToRemove);
        await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"成功将用户 {qqToRemove} 从黑名单中移除。")]);
        return true;
    }
}