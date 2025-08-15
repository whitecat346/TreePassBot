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
}