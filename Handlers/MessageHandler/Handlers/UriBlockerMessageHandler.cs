using System.Text;
using System.Text.RegularExpressions;
using Makabaka.Events;
using Makabaka.Messages;
using Makabaka.Models;
using Microsoft.Extensions.Logging;
using TreePassBot.Services.Interfaces;
using IMessageHandler = TreePassBot.Handlers.MessageHandler.Interfaces.IMessageHandler;

namespace TreePassBot.Handlers.MessageHandler.Handlers;

public partial class UriBlockerMessageHandler(
    IMessageService messageService,
    ILogger<UriBlockerMessageHandler> logger) : IMessageHandler
{
    [GeneratedRegex(@"(?:https?://)?(?:[a-zA-Z0-9_-]+\.)+[a-zA-Z]{2,6}(?::[0-9]+)?(?:/[^\s]*)?")]
    private static partial Regex UriMatcherRegex();

    /// <inheritdoc />
    public async Task InvokeAsync(GroupMessageEventArgs e, HandlerLinkNode? next)
    {
        if (IsAdmin(e.Sender))
        {
            next?.Next(e);
            return;
        }

        var regex = UriMatcherRegex();
        var msg = await MessageToStringAsync(e.Message).ConfigureAwait(false);

        if (!regex.IsMatch(msg))
        {
            next?.Next(e);
            return;
        }

        await messageService.DeleteMessageAsync(e.MessageId).ConfigureAwait(false);
        await e.ReplyAsync([
            new AtSegment(e.UserId),
            new TextSegment("由于管理员设置，该群禁止发送链接，消息已被删除。")
        ]).ConfigureAwait(false);

        logger.LogInformation("Delete message {Id} from {GroupId}.", e.MessageId, e.GroupId);
    }

    private async Task<string> MessageToStringAsync(Message messages)
    {
        var sb = await SwitchSegmentAsync(messages, new StringBuilder());

        return sb.ToString();
    }

    private async Task<string> ForwardMessageToStringAsync(string forwardId)
    {
        var response = await messageService.GetForwardMessageAsync(forwardId).ConfigureAwait(false);
        if (response is null)
        {
            return string.Empty;
        }

        var sb = await SwitchSegmentAsync(response.Message, new StringBuilder());

        return sb.ToString();
    }

    private async Task<StringBuilder> SwitchSegmentAsync(Message messages, StringBuilder sb)
    {
        foreach (var msg in messages)
        {
            switch (msg)
            {
                case TextSegment textSegment:
                    sb.Append(textSegment.Data.Text);
                    break;
                case AtSegment atSegment:
                    sb.Append($"@{atSegment.Data.Name ?? string.Empty}");
                    break;
                case ForwardSegment forwardSegment:
                    sb.Append(await ForwardMessageToStringAsync(forwardSegment.Data.Id).ConfigureAwait(false));
                    break;
            }
        }

        return sb;
    }

    private bool IsAdmin(GroupMessageSenderInfo? sender)
    {
        if (sender == null)
        {
            return false;
        }

        if (sender.Role is GroupRoleType.Member)
        {
            return false;
        }

        return true;
    }
}