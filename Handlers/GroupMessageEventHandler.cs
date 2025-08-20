using System.Text.RegularExpressions;
using Makabaka.Events;
using Makabaka.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Data;
using TreePassBot.Data.Entities;
using TreePassBot.Handlers.AdminCommands;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;
using TreePassBot.Utils;

namespace TreePassBot.Handlers;

public partial class GroupMessageEventHandler(
    IAuditService auditService,
    JsonDataStore dataStore,
    CommandDispatcher commandDispatcher,
    IOptions<BotConfig> config,
    PasscodeGeneratorUtil generator,
    ArgumentsSpiliterUtil spiliter,
    ILogger<GroupMessageEventHandler> logger)
{
    private readonly BotConfig _config = config.Value;
    private static readonly Regex AuditCommandRegex = AuditorCommandRegexFunc();

    public async Task HandleGroupMessage(GroupMessageEventArgs e)
    {
        var msg = e.Message.ToString().Trim();

        logger.LogInformation("Received group message from {GroupId} by {UserId}: {Message}",
                              e.GroupId, e.UserId, msg);

        if (_config.AuditorQqIds.Contains(e.UserId) && e.Message[0] is AtSegment)
        {
            await HandleAuditorCommandAsync(e);
            return;
        }

        await HandleAdminCommandAsync(e);
    }

    [Obsolete]
    private async Task HandleRefreshPasscodeAsync(GroupMessageEventArgs e)
    {
        var user = dataStore.GetUserByQqId(e.UserId);
        if (user == null)
        {
            return;
        }

        if (user.Status == AuditStatus.Expired)
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
        await commandDispatcher.ExecteAsync(e);
    }

    private async Task HandleAuditorCommandAsync(GroupMessageEventArgs e)
    {
        if (_config.AuditGroupId != e.GroupId)
        {
            return;
        }

        var msg = e.Message.ToString();
        var match = AuditCommandRegex.Match(msg);
        if (!match.Success)
        {
            return;
        }

        logger.LogInformation("Auditor {Id} issued auditor command: {Content}", e.UserId, msg);

        var targetIdStr = match.Groups[1].Value;

        try
        {
            var targetQqIds = spiliter.SpilitArguments(targetIdStr)
                                      .Select(ulong.Parse)
                                      .ToHashSet();

            var command = match.Groups[2].Value.ToLower();

            Message replyMsg;
            if (command.Equals("pass", StringComparison.OrdinalIgnoreCase))
            {
                replyMsg = await PassCommandHandler(targetQqIds, e.UserId, e.GroupId);
            }
            else if (command.Equals("deny", StringComparison.OrdinalIgnoreCase))
            {
                replyMsg = await DenyCommandHandler(targetQqIds, e.UserId, e.GroupId);
            }
            else
            {
                replyMsg = [new AtSegment(e.UserId), new TextSegment("未知的执行分支，请使用 `pass` 或 `deny`。")];
            }

            await e.ReplyAsync(replyMsg);
        }
        catch (FormatException formatException)
        {
            await e.ReplyAsync([
                new AtSegment(e.UserId),
                new TextSegment("输入的QQ号格式错误，请检查你的输入。\n"),
                new TextSegment($"错误信息为：{formatException.Message}")
            ]);
            logger.LogError("Invalid format for QQ number: {Msg}", formatException.Message);
        }
        catch (OverflowException overflow)
        {
            await e.ReplyAsync([
                new AtSegment(e.UserId),
                new TextSegment("输入的QQ号超出了ulong上限，发生了整形溢出。\n"),
                new TextSegment($"错误信息为：{overflow.Message}")
            ]);
            logger.LogError("Occurred overflow exception when try to convert string to ulong: {Msg}", overflow.Message);
        }
        catch (Exception exception)
        {
            await e.ReplyAsync([
                new AtSegment(e.UserId),
                new TextSegment("发生了未知错误。\n"),
                new TextSegment($"错误信息为：{exception.Message}")
            ]);
            logger.LogError("Caught an unknow exception: {Msg}", exception);
        }
    }

    [GeneratedRegex(@"((?:\d+\s+)+)(pass|deny)", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex AuditorCommandRegexFunc();

    private async Task<Message> PassCommandHandler(HashSet<ulong> targetIds, ulong operatorId, ulong groupId)
    {
        List<ulong> failedQqIds = [];

        foreach (var targetQqId in targetIds)
        {
            var success = await auditService.ProcessApprovalAsync(targetQqId, operatorId, groupId);
            if (!success)
            {
                failedQqIds.Add(targetQqId);
            }
        }

        if (failedQqIds.Count == 0)
        {
            return [new AtSegment(operatorId), new TextSegment("已通过所有指定用户的审核!")];
        }

        var failedQqIdStr = string.Join('\n', failedQqIds);
        return [new AtSegment(operatorId), new TextSegment($"以下用户处理失败: {failedQqIdStr}")];
    }

    private async Task<Message> DenyCommandHandler(HashSet<ulong> targetIds, ulong operatorId, ulong groupId)
    {
        List<ulong> failedQqIds = [];

        foreach (var targetQqId in targetIds)
        {
            var success = await auditService.ProcessDenialAsync(targetQqId, operatorId, groupId);
            if (!success)
            {
                failedQqIds.Add(targetQqId);
            }
        }

        if (failedQqIds.Count == 0)
        {
            return [new AtSegment(operatorId), new TextSegment("已拒绝所有指定用户的审核!")];
        }
        else
        {
            var failedQqIdStr = string.Join("\n", failedQqIds);
            return [new AtSegment(operatorId), new TextSegment($"以下用户处理失败: {failedQqIdStr}")];
        }
    }
}