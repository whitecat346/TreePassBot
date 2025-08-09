using System.Text.RegularExpressions;
using Makabaka.Events;
using Microsoft.Extensions.Options;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers;

public class PrivateMessageEventHandler(
    IAuditService auditService,
    IOptions<BotConfig> config)
{
    private readonly BotConfig _config = config.Value;
    private static readonly Regex CommandRegex = new(@"^(\d+)\s+(pass|deny)$", RegexOptions.IgnoreCase);

    public async Task HandlePrivateMessage(object sender, PrivateMessageEventArgs e)
    {
        if (!_config.AuditorQQIds.Contains(e.Sender.UserId))
        {
            return;
        }

        var match = CommandRegex.Match(e.Message.ToString() ?? string.Empty);
        if (!match.Success)
        {
            return;
        }

        var targetQqId = ulong.Parse(match.Groups[1].Value);
        var command = match.Groups[2].Value.ToLower();

        if (command.Equals("pass", StringComparison.OrdinalIgnoreCase))
        {
            await auditService.ProcessApprovalAsync(targetQqId, e.Sender.UserId);
        }
        else if (command.Equals("deny", StringComparison.OrdinalIgnoreCase))
        {
            await auditService.ProcessDenialAsync(targetQqId, e.Sender.UserId);
        }
    }
}