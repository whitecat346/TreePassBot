using System.Text.RegularExpressions;
using Makabaka.Events;
using Makabaka.Messages;
using Microsoft.Extensions.Options;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers;

public partial class GroupMessageEventHandler(
    IAuditService auditService,
    IOptions<BotConfig> config)
{
    private readonly BotConfig _config = config.Value;
    private static readonly Regex CommandRegex = AuditorCommandRegex();

    public async Task HandleGroupMessage(object sender, GroupMessageEventArgs e)
    {
        if (e.Sender == null)
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
            var success = await auditService.ProcessApprovalAsync(targetQqId, e.Sender.UserId, e.GroupId);

            if (success)
            {
                await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("已批准!")]);
            }
        }
        else if (command.Equals("deny", StringComparison.OrdinalIgnoreCase))
        {
            await auditService.ProcessDenialAsync(targetQqId, e.Sender.UserId);
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("已拒绝!")]);
        }
        else
        {
            await e.ReplyAsync(
                [new AtSegment(e.UserId), new TextSegment("未知的命令，请使用 `pass` 或 `deny`。")]);
        }
    }

    [GeneratedRegex(@"^(\d+)\s+(pass|deny)$", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex AuditorCommandRegex();
}