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

        //        if (e.Message[0] is AtSegment && e.Message.Count == 1)
        //        {
        //#if DEBUG
        //            logger.LogInformation("Handle refresh passcode command: {msg}", msg);
        //#endif

        //            await HandleRefreshPasscodeAsync(e);
        //        }

#if DEBUG
        logger.LogInformation("Do nothing.");
#endif
    }

    [Obsolete]
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
        await commandDispatcher.ExecteAsync(e);
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

        var targetQqIdStr = match.Groups[1].Value;

        try
        {
            var targetQqIds = targetQqIdStr.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                           .SelectMany(it => it.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                                           .Where(it => !string.IsNullOrWhiteSpace(it))
                                           .Select(ulong.Parse)
                                           .ToHashSet();

            var command = match.Groups[2].Value.ToLower();

            if (command.Equals("pass", StringComparison.OrdinalIgnoreCase))
            {
                List<ulong> failedQqIds = [];

                foreach (var targetQqId in targetQqIds)
                {
                    var success = await auditService.ProcessApprovalAsync(targetQqId, e.UserId, e.GroupId);
                    if (!success)
                    {
                        failedQqIds.Add(targetQqId);
                    }
                }

                if (failedQqIds.Count == 0)
                {
                    await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("已通过所有指定用户的审核!")]);
                }
                else
                {
                    var failedQqIdStr = string.Join("\n", failedQqIds);
                    await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"以下用户处理失败: {failedQqIdStr}")]);
                }
            }
            else if (command.Equals("deny", StringComparison.OrdinalIgnoreCase))
            {
                List<ulong> failedQqIds = [];

                foreach (var targetQqId in targetQqIds)
                {
                    var success = await auditService.ProcessDenialAsync(targetQqId, e.UserId, e.GroupId);
                    if (!success)
                    {
                        failedQqIds.Add(targetQqId);
                    }
                }

                if (failedQqIds.Count == 0)
                {
                    await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("已拒绝所有指定用户的审核!")]);
                }
                else
                {
                    var failedQqIdStr = string.Join("\n", failedQqIds);
                    await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment($"以下用户处理失败: {failedQqIdStr}")]);
                }
            }
            else
            {
                await e.ReplyAsync(
                    [new AtSegment(e.UserId), new TextSegment("未知的命令，请使用 `pass` 或 `deny`。")]);
            }
        }
        catch (Exception exception)
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("在尝试转换字符串时发生了错误，你是不是输入了错误的QQ号呢？")]);
            logger.LogError("Failed to convert string to ulong: {Msg}", exception.Message);
        }
    }

    [GeneratedRegex(@"((?:\d+\s+)+)(pass|deny)", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex AuditorCommandRegex();
}