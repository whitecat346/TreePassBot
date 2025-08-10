using System.Text.RegularExpressions;
using Makabaka.Events;
using Makabaka.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Data;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers;

public partial class GroupMessageEventHandler(
    IAuditService auditService,
    IUserService userService,
    IMessageService messageService,
    JsonDataStore dataStore,
    IOptions<BotConfig> config,
    ILogger<GroupMessageEventHandler> logger)
{
    private readonly BotConfig _config = config.Value;
    private static readonly Regex CommandRegex = AuditorCommandRegex();

    public async Task HandleGroupMessage(GroupMessageEventArgs e)
    {
        var msg = e.Message.ToString().Trim();

        if (msg.StartsWith('.'))
        {
            await HandleAdminCommandAsync(e);
            return;
        }

        await HandleAuditorCommandAsync(e);
    }

    private async Task HandleAdminCommandAsync(GroupMessageEventArgs e)
    {
        if (_config.AdminQqId  != e.UserId)
        {
            return;
        }

        var message = e.Message.ToString();
        var groupId = e.GroupId;
        var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower(); // e.g., ".rand"

        logger.LogInformation("Admin {AdminId} issued test command: {Command}", groupId, message);

        switch (command)
        {
            case ".rand":
                await TestRandomGeneration(groupId);
                break;

            case ".check":
                if (parts.Length < 2 || !ulong.TryParse(parts[1], out var qqToCheck))
                {
                    await messageService.SendGroupMessageAsync(groupId, [new TextSegment("用法: .check [QQ号]")]);
                    return;
                }

                await TestCheckUser(groupId, qqToCheck);
                break;

            case ".addtest":
                if (parts.Length < 2 || !ulong.TryParse(parts[1], out var qqToAdd))
                {
                    await messageService.SendGroupMessageAsync(groupId, [new TextSegment("用法: .addtest [QQ号]")]);
                    return;
                }

                await TestAddUser(groupId, qqToAdd);
                break;

            case ".help":
                const string helpText = "可用测试命令:\n" +
                                        ".rand - 测试生成一个唯一的10位验证码\n" +
                                        ".check [QQ号] - 查看指定用户的审核状态\n" +
                                        ".addtest [QQ号] - 手动添加一个用户到待审核列表";
                await messageService.SendGroupMessageAsync(groupId, [new TextSegment(helpText)]);
                break;

            default:
                await messageService.SendGroupMessageAsync(groupId,
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

        if (_config.AuditorQqIds.Contains(e.UserId))
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("您不是审核员！")]);
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

    #endregion
}