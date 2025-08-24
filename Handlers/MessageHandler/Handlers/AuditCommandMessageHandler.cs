using System.Text.RegularExpressions;
using Makabaka.Events;
using Makabaka.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;
using TreePassBot.Utils;
using IMessageHandler = TreePassBot.Handlers.MessageHandler.Interfaces.IMessageHandler;

namespace TreePassBot.Handlers.MessageHandler.Handlers;

public partial class AuditCommandMessageHandler(
    ILogger<AuditCommandMessageHandler> logger,
    ArgumentsSpiliterUtil spiliter,
    IAuditService auditService,
    IOptions<BotConfig> config) : IMessageHandler
{
    private readonly Regex _auditCommandRegex = AuditorCommandRegexFunc();
    private readonly BotConfig _config = config.Value;

    public async Task InvokeAsync(GroupMessageEventArgs e, HandlerLinkNode? next)
    {
        if (_config.AuditGroupId != e.GroupId)
        {
            next?.Next(e);
            return;
        }

        if (!_config.AuditorQqIds.Contains(e.UserId))
        {
            next?.Next(e);
            return;
        }

        if (e.Message[0] is not AtSegment)
        {
            next?.Next(e);
            return;
        }

        var msg = e.Message.ToString();
        var match = _auditCommandRegex.Match(msg);
        if (!match.Success)
        {
            next?.Next(e);
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
                replyMsg = await PassCommandHandler(targetQqIds, e.UserId, e.GroupId).ConfigureAwait(false);
            }
            else if (command.Equals("deny", StringComparison.OrdinalIgnoreCase))
            {
                replyMsg = await DenyCommandHandler(targetQqIds, e.UserId, e.GroupId).ConfigureAwait(false);
            }
            else
            {
                replyMsg = [new AtSegment(e.UserId), new TextSegment("未知的执行分支，请使用 `pass` 或 `deny`。")];
            }

            await e.ReplyAsync(replyMsg).ConfigureAwait(false);
        }
        catch (FormatException formatException)
        {
            await e.ReplyAsync([
                new AtSegment(e.UserId),
                new TextSegment("输入的QQ号格式错误，请检查你的输入。\n"),
                new TextSegment($"错误信息为：{formatException.Message}")
            ]).ConfigureAwait(false);
            logger.LogError("Invalid format for QQ number: {Msg}", formatException.Message);
        }
        catch (OverflowException overflow)
        {
            await e.ReplyAsync([
                new AtSegment(e.UserId),
                new TextSegment("输入的QQ号超出了ulong上限，发生了整形溢出。\n"),
                new TextSegment($"错误信息为：{overflow.Message}")
            ]).ConfigureAwait(false);
            logger.LogError("Occurred overflow exception when try to convert string to ulong: {Msg}", overflow.Message);
        }
        catch (Exception exception)
        {
            await e.ReplyAsync([
                new AtSegment(e.UserId),
                new TextSegment("发生了未知错误。\n"),
                new TextSegment($"错误信息为：{exception.Message}")
            ]).ConfigureAwait(false);
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
            var success = await auditService.ProcessApprovalAsync(targetQqId, operatorId, groupId)
                                            .ConfigureAwait(false);
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
            var success = await auditService.ProcessDenialAsync(targetQqId, operatorId, groupId)
                                            .ConfigureAwait(false);
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