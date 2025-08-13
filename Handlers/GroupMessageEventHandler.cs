using System.Text.RegularExpressions;
using Makabaka.Events;
using Makabaka.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Data;
using TreePassBot.Data.Entities;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;
using TreePassBot.Utils;

namespace TreePassBot.Handlers;

public partial class GroupMessageEventHandler(
    IAuditService auditService,
    IUserService userService,
    IMessageService messageService,
    JsonDataStore dataStore,
    IOptions<BotConfig> config,
    PasscodeGeneratorUtil generator,
    ILogger<GroupMessageEventHandler> logger)
{
    private readonly BotConfig _config = config.Value;
    private static readonly Regex CommandRegex = AuditorCommandRegex();

    public async Task HandleGroupMessage(GroupMessageEventArgs e)
    {
        var msg = e.Message.ToString().Trim();

#if DEBUG
        logger.LogInformation("Received group message from {GroupId} by {UserId}: {Message}",
            e.GroupId, e.UserId, msg);
#endif

        if (_config.AuditorQqIds.Contains(e.UserId) && e.Message[0] is AtSegment)
        {
#if DEBUG
            logger.LogInformation("Handle auditor command: {message}", msg);
#endif

            await HandleAuditorCommandAsync(e);
            return;
        }

        if (_config.AdminQqIds.Contains(e.UserId) && msg.StartsWith('.'))
        {
#if DEBUG
            logger.LogInformation("Handle admin command: {msg}", msg);
#endif

            await HandleAdminCommandAsync(e);
            return;
        }

        if (e.Message[0] is AtSegment && e.Message.Count == 1)
        {
#if DEBUG
            logger.LogInformation("Handle refresh passcode command: {msg}", msg);
#endif

            await HandleRefreshPasscodeAsync(e);
        }

#if DEBUG
        logger.LogInformation("Do nothing.");
#endif
    }

    private async Task HandleRefreshPasscodeAsync(GroupMessageEventArgs e)
    {
        var user = dataStore.GetUserByQqId(e.UserId);
        if (user == null)
        {
            return;
        }

        if (user.Status == AuditStatus.Expried)
        {
            var passcode = await generator.GenerateUniquePasscodeAsync();
            user.Passcode = passcode;
            user.Status = AuditStatus.Approved;
            user.ExpriedAt = DateTime.UtcNow + TimeSpan.FromMinutes(10);

            dataStore.UpdateUser(user);

            await e.ReplyAsync([
                new AtSegment(e.UserId),
                new TextSegment("您的新验证码是："),
                new TextSegment(user.Passcode)
            ]);

            logger.LogInformation("User {qqId} passed expried, generating new passcode: {passcode}.", e.UserId,
                passcode);
        }
    }

    private async Task HandleAdminCommandAsync(GroupMessageEventArgs e)
    {
        var message = e.Message.ToString();
        var groupId = e.GroupId;
        var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower(); // e.g., ".rand"

        logger.LogInformation("Admin {AdminId} issued test command: {Command}", e.UserId, message);

        switch (command)
        {
            case ".rand":
#if DEBUG
                logger.LogInformation("In rand test command.");
#endif
                await TestRandomGeneration(groupId);
                break;

            case ".check":
#if DEBUG
                logger.LogInformation("In check test command.");
#endif
                if (parts.Length < 2 || !ulong.TryParse(parts[1], out var qqToCheck))
                {
                    await e.ReplyAsync([new TextSegment("用法: .check [QQ号]")]);
                    return;
                }

                await TestCheckUser(groupId, qqToCheck);
                break;

            case ".addtest":
#if DEBUG
                logger.LogInformation("In addtest test command.");
#endif
                if (parts.Length < 2 || !ulong.TryParse(parts[1], out var qqToAdd))
                {
                    await e.ReplyAsync([new TextSegment("用法: .addtest [QQ号]")]);
                    return;
                }

                await TestAddUser(groupId, qqToAdd);
                break;

            case ".reset":
#if DEBUG
                logger.LogInformation("In reset test command.");
#endif
                if (parts.Length < 2 || !ulong.TryParse(parts[1], out var qqToReset))
                {
                    await e.ReplyAsync([new TextSegment("用法: .reset [QQ号]")]);
                    return;
                }

                await TestResetUser(groupId, qqToReset);
                await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("用户审核状态已重置。")]);
                break;

            case ".audit-help":
#if DEBUG
                logger.LogInformation("In audit-help test command.");
#endif
                const string auditHelpText = "审核员命令:\n" +
                                             "使用 @+QQ号 pass - 通过指定用户的审核\n" +
                                             "使用 @+QQ号 deny - 拒绝指定用户的审核";
                await e.ReplyAsync([new TextSegment(auditHelpText)]);
                break;

            case ".links":
#if DEBUG
                logger.LogInformation("In links test command.");
#endif
                var msg = string.Join('\n', _config.QuestionnaireLinks);
                await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment(msg)]);
                break;

            case ".help":
#if DEBUG
                logger.LogInformation("In help test command.");
#endif
                const string helpText = "可用命令（仅限管理员使用）:\n" +
                                        ".rand - 测试生成一个唯一的10位验证码\n" +
                                        ".check [QQ号] - 查看指定用户的审核状态\n" +
                                        ".addtest [QQ号] - 手动添加一个用户到待审核列表\n" +
                                        ".reset [QQ号] - 重置指定用户的审核状态\n" +
                                        ".links - 查看审核表单链接\n" +
                                        ".audit-help - 查看审核相关命令";
                await e.ReplyAsync([new TextSegment(helpText)]);
                break;

            default:
#if DEBUG
                logger.LogInformation("In unknow command.");
#endif
                await e.ReplyAsync(
                    [new TextSegment($"未知命令: {command}。发送 .help 查看帮助。")]);
                break;
        }
    }

    private async Task HandleAuditorCommandAsync(GroupMessageEventArgs e)
    {
        if (e.Message[0] is not AtSegment)
        {
            return;
        }

        if (_config.AuditGroupId != e.GroupId)
        {
            return;
        }

        var match = CommandRegex.Match(e.Message.ToString());
        if (!match.Success)
        {
            return;
        }

        var targetQqId = ulong.Parse(match.Groups[1].Value);

        var command = match.Groups[2].Value.ToLower();

        if (command.Equals("pass", StringComparison.OrdinalIgnoreCase))
        {
            var success = await auditService.ProcessApprovalAsync(targetQqId, e.UserId, e.GroupId);

            if (success)
            {
                await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("已批准!")]);
            }
        }
        else if (command.Equals("deny", StringComparison.OrdinalIgnoreCase))
        {
            var success = await auditService.ProcessDenialAsync(targetQqId, e.UserId, e.GroupId);

            if (success)
            {
                await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("已拒绝!")]);
            }
        }
        else
        {
            await e.ReplyAsync(
                [new AtSegment(e.UserId), new TextSegment("未知的命令，请使用 `pass` 或 `deny`。")]);
        }
    }

    [GeneratedRegex(@"(\d+)\s+(pass|deny)$", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex AuditorCommandRegex();

    #region Test Command

    private async Task TestRandomGeneration(ulong groupId)
    {
        var passcode = new string(Enumerable.Range(0, 10)
            .Select(_ => (char)Random.Shared.Next('0', '9' + 1))
            .ToArray());

        var isUnique = !dataStore.PasscodeExists(passcode);

        await messageService.SendGroupMessageAsync(groupId,
            [new TextSegment($"生成测试验证码: {passcode}\n在当前数据中是否唯一: {isUnique}")]);
    }

    private async Task TestCheckUser(ulong groupId, ulong qqToCheck)
    {
        var user = await userService.GetPendingUserAsync(qqToCheck);
        if (user == null)
        {
            await messageService.SendGroupMessageAsync(groupId, [new TextSegment($"用户 {qqToCheck} 未在数据存储中找到。")]);
            return;
        }

        var userInfo = $"查询用户: {user.QqId}\n" +
                       $"状态: {user.Status}\n" +
                       $"验证码: {(string.IsNullOrEmpty(user.Passcode) ? "无" : user.Passcode)}\n" +
                       $"创建时间: {user.CreatedAt:yyyy-MM-dd HH:mm:ss} (UTC)\n" +
                       $"更新时间: {user.UpdatedAt:yyyy-MM-dd HH:mm:ss} (UTC)";

        await messageService.SendGroupMessageAsync(groupId, [new TextSegment(userInfo)]);
    }

    private async Task TestAddUser(ulong groupId, ulong qqToAdd)
    {
        var success = await userService.AddPendingUserAsync(qqToAdd);
        if (success)
        {
            await messageService.SendGroupMessageAsync(groupId, [new TextSegment($"成功将用户 {qqToAdd} 添加到待审核列表。")]);
        }
        else
        {
            await messageService.SendGroupMessageAsync(groupId, [new TextSegment($"无法添加用户 {qqToAdd}，可能已存在或发生错误。")]);
        }
    }

    private async Task TestResetUser(ulong groupId, ulong qqToReset)
    {
        await userService.DeleteUserAsync(qqToReset);
        var success = await userService.AddPendingUserAsync(qqToReset);

        if (success)
        {
            await messageService.SendGroupMessageAsync(groupId, [new TextSegment($"成功将用户 {qqToReset} 重置为待审核状态。")]);
        }
        else
        {
            await messageService.SendGroupMessageAsync(groupId, [new TextSegment($"无法重置用户 {qqToReset}，可能已存在或发生错误。")]);
        }
    }

    #endregion
}