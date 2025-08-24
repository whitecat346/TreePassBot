using System.Text;
using Makabaka.Events;
using Makabaka.Messages;
using Makabaka.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Data;
using TreePassBot.Data.Entities;
using TreePassBot.Handlers.Commands.Data;
using TreePassBot.Handlers.Commands.Permission;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers.Commands.Commands;

public class AdminCommands(
    JsonDataStore dataStore,
    IUserService userService,
    IMessageService messageService,
    ILogger<AdminCommands> logger,
    IOptions<BotConfig> config)
{
    [BotCommand("check", Description = "查询用户状态", Usage = ".check [QQ号]")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> CheckUser(GroupMessageEventArgs e)
    {
        var spilied = e.Message.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (!ulong.TryParse(spilied[1], out var qqToCheck))
        {
            return false;
        }

        var user = await userService.GetUserInfoByIdAsync(qqToCheck).ConfigureAwait(false);
        if (user == null)
        {
            var isInBlackList = await userService.IsInBlackList(qqToCheck).ConfigureAwait(false);
            if (isInBlackList)
            {
                await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"用户 {qqToCheck} 位于黑名单中。")])
                       .ConfigureAwait(false);
                return true;
            }

            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"用户 {qqToCheck} 未在数据存储中找到。")])
                   .ConfigureAwait(false);
            return true;
        }

        var userInfo = new StringBuilder($"查询用户: {user.QqId}\n" +
                                         $"状态: {user.Status}\n" +
                                         $"验证码: {(string.IsNullOrEmpty(user.Passcode) ? "无" : user.Passcode)}\n" +
                                         $"创建时间: {user.CreatedAt:yyyy-MM-dd HH:mm:ss} (UTC)\n" +
                                         $"更新时间: {user.UpdatedAt:yyyy-MM-dd HH:mm:ss} (UTC)");

        if (user.ExpriedAt is not null)
        {
            userInfo.AppendLine($"过期时间: {user.ExpriedAt:yyyy-MM-dd HH:mm:ss} (UTC)");
        }

        await e.ReplyAsync([new TextSegment(userInfo.ToString())]).ConfigureAwait(false);
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

        var success = await userService.AddPendingUserAsync(qqToAdd).ConfigureAwait(false);
        if (success)
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"成功将用户 {qqToAdd} 添加到待审核列表。")])
                   .ConfigureAwait(false);
        }
        else
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"无法添加用户 {qqToAdd}，可能已存在或发生错误。")])
                   .ConfigureAwait(false);
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

        await userService.DeleteUserAsync(qqToReset).ConfigureAwait(false);
        var success = await userService.AddPendingUserAsync(qqToReset).ConfigureAwait(false);

        if (success)
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"成功将用户 {qqToReset} 重置为待审核状态。")])
                   .ConfigureAwait(false);
        }
        else
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"无法重置用户 {qqToReset}，可能已存在或发生错误。")])
                   .ConfigureAwait(false);
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
        await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment(auditHelpText)])
               .ConfigureAwait(false);
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

        await userService.AddToBlackList(qqToAdd).ConfigureAwait(false);

        await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"成功将用户 {qqToAdd} 添加到黑名单。")])
               .ConfigureAwait(false);

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

        await userService.RemoveFromBlackList(qqToRemove).ConfigureAwait(false);
        await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"成功将用户 {qqToRemove} 从黑名单中移除。")])
               .ConfigureAwait(false);
        return true;
    }

    [BotCommand("retake", Description = "将未在名单中的用户重新加入", Usage = ".retake")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> RetakeUser(GroupMessageEventArgs e)
    {
        var targetGroupId = config.Value.AuditGroupId;

        var userList = await messageService.GetGroupMemberList(targetGroupId).ConfigureAwait(false);
        if (userList == null)
        {
            logger.LogWarning("Failed to get group member list.");
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("无法获取用户列表，可能是该群组没有成员或发生了错误。")])
                   .ConfigureAwait(false);
            return false;
        }

        var botConfig = config.Value;
        HashSet<ulong> adminQqIds = [..botConfig.AdminQqIds, ..botConfig.AuditorQqIds, botConfig.BotQqId];

        var withOutAdmin = userList
                          .Where(user => user.Role is GroupRoleType.Member)
                          .Where(user => !adminQqIds.Contains(user.UserId));

        var existingUserIds = dataStore.GetAllUsers().Select(user => user.QqId).ToHashSet();
        var notInList = withOutAdmin
                       .Where(user => !existingUserIds.Contains(user.UserId))
                       .Select(user => user.UserId)
                       .ToList();

        if (notInList.Count == 0)
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("没有用户需要重新加入。")]).ConfigureAwait(false);
            return true;
        }

        foreach (var userId in notInList)
        {
            await userService.AddPendingUserAsync(userId).ConfigureAwait(false);
        }

        await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"共有{notInList.Count}个用户被重新加入。")])
               .ConfigureAwait(false);

        return true;
    }

    [BotCommand("list-duplicated", Description = "列出已在大群的用户", Usage = ".list-duplicated")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> ListDuplicatedUsers(GroupMessageEventArgs e)
    {
        var botConfig = config.Value;

        var targetGroupIds = botConfig.MainGroupIds;
        var tasks = targetGroupIds.Select(async groupId =>
        {
            var members = await messageService.GetGroupMemberList(groupId).ConfigureAwait(false);
            if (members != null)
            {
                return members;
            }

            logger.LogWarning("Failed to get group {GroupId} member list info.", groupId);
            return Enumerable.Empty<GroupMemberInfo>();
        }).ToList();

        // 并发等待所有任务完成
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // 将所有结果合并到一个列表中
        var mainUsers = results.SelectMany(x => x).ToList();

        if (mainUsers.Count == 0)
        {
            logger.LogWarning("Failed to get group group member list.");
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("获取用户列表时失败。")]).ConfigureAwait(false);
            return false;
        }

        var auditUsers = await messageService.GetGroupMemberList(botConfig.AuditGroupId).ConfigureAwait(false);
        if (auditUsers == null)
        {
            logger.LogWarning("Failed to get audit group member list.");
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("无法获取审核群组的用户列表。")]).ConfigureAwait(false);
            return false;
        }


        HashSet<ulong> adminQqIds = [..botConfig.AdminQqIds, ..botConfig.AuditorQqIds, botConfig.BotQqId];

        var auditGroupUserDict = auditUsers
                                .Where(user => user.Role is GroupRoleType.Member)
                                .Where(user => !adminQqIds.Contains(user.UserId))
                                .ToDictionary(user => user.UserId);

        var mainGroupUserIds = mainUsers.Select(user => user.UserId).ToHashSet();

        var duplicatedUserIds = auditGroupUserDict.Keys.Where(id => mainGroupUserIds.Contains(id)).ToList();

        if (duplicatedUserIds.Count == 0)
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("没有找到重复的用户。")]).ConfigureAwait(false);
            return true;
        }

        var response = new StringBuilder("以下已在大群的用户仍在审核群中:\n");
        foreach (var userId in duplicatedUserIds)
        {
            var userInfo = auditGroupUserDict[userId];
            response.AppendLine($"{userInfo.Nickname} - {userInfo.UserId}");
        }

        await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment(response.ToString())]).ConfigureAwait(false);
        return true;
    }

    [BotCommand("list-expired", Description = "列出验证码过期的用户", Usage = ".list-expired")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> ListExpired(GroupMessageEventArgs e)
    {
        var users = dataStore.GetAllUsers();
        var expiredUsers = users.Where(user => user.Status is AuditStatus.Expired).ToList();
        var sb = new StringBuilder("以下用户的审核已过期：\n");
        foreach (var user in expiredUsers)
        {
            sb.AppendLine($"{user.QqId}");
        }

        await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment(sb.ToString())]).ConfigureAwait(false);
        return true;
    }

    [BotCommand("rm-unexist", Description = "移除不存在的用户", Usage = ".rm-unexist")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> RemoveUnexistUsers(GroupMessageEventArgs e)
    {
        var botConfig = config.Value;

        var existUsersResponse = await messageService.GetGroupMemberList(botConfig.AuditGroupId);
        if (existUsersResponse == null)
        {
            logger.LogWarning("Failed to get audit group member list.");
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("无法获取审核群组的用户列表。")]).ConfigureAwait(false);
            return false;
        }

        HashSet<ulong> adminQqIds = [..botConfig.AdminQqIds, ..botConfig.AuditorQqIds, botConfig.BotQqId];

        var existUserIds = existUsersResponse
                          .Where(user => user.Role is GroupRoleType.Member)
                          .Where(user => !adminQqIds.Contains(user.UserId))
                          .Select(user => user.UserId)
                          .ToHashSet();

        var inPendingListUsers = dataStore.GetAllUsers()
                                          .Select(user => user.QqId)
                                          .ToHashSet();

        var notInListUsers = inPendingListUsers.Where(id => !existUserIds.Contains(id)).ToList();

        if (notInListUsers.Count == 0)
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("没有需要移除的用户。")]).ConfigureAwait(false);
            return true;
        }

        foreach (var userId in notInListUsers)
        {
            await userService.DeleteUserAsync(userId).ConfigureAwait(false);
        }

        await e.ReplyAsync([
            new AtSegment(e.UserId),
            new TextSegment($"操作完成，已从待审核列表中移除了 {notInListUsers.Count} 名不存在的用户。")
        ]).ConfigureAwait(false);

        return true;
    }

    [BotCommand("rm-user", Description = "从名单中移除指定的用户", Usage = ".rm-user [QQ号]")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> RemoveUser(GroupMessageEventArgs e)
    {
        var spilied = e.Message.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (spilied.Length < 2 || !ulong.TryParse(spilied[1], out var qqToRemove))
        {
            return false;
        }

        await userService.DeleteUserAsync(qqToRemove).ConfigureAwait(false);
        await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("成功将指定的用户从名单中移除！")]).ConfigureAwait(false);
        return true;
    }
}