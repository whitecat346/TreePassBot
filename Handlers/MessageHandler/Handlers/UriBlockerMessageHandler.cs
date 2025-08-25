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

    private readonly Regex _uriMatcher = UriMatcherRegex();

    /// <inheritdoc />
    public async Task InvokeAsync(GroupMessageEventArgs e, HandlerLinkNode? next)
    {
#if !DEBUG
        if (IsAdmin(e.Sender))
        {
            next?.Next(e);
            return;
        }
#endif


        var msg = await MessageToStringAsync(e.Message).ConfigureAwait(false);
#if DEBUG
        logger.LogInformation("Converted message content: {Content}", msg);
#endif

        if (!_uriMatcher.IsMatch(msg))
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

        // node to normal segment
        Message msgContent = [];
        foreach (var node in response.Message.Select(node => node as NodeSegment))
        {
            ArgumentNullException.ThrowIfNull(node);

            var content = node.Data.Content!;

            msgContent.AddRange(content);
        }

        var sb = await SwitchSegmentAsync(msgContent, new StringBuilder());

        return sb.ToString();
    }

    private async Task<StringBuilder> SwitchSegmentAsync(Message messages, StringBuilder sb)
    {
        foreach (var msg in messages)
        {
            switch (msg)
            {
                case TextSegment textSegment:

#if DEBUG
                    logger.LogInformation("Get text message.");
#endif

                    sb.AppendLine(textSegment.Data.Text);
                    break;
                case AtSegment atSegment:

#if DEBUG
                    logger.LogInformation("Get at message.");
#endif

                    sb.AppendLine($"{atSegment.Data.Name ?? string.Empty}");
                    break;
                case ForwardSegment forwardSegment:
                    var forwardMsgContent =
                        await ForwardMessageToStringAsync(forwardSegment.Data.Id).ConfigureAwait(false);

#if DEBUG
                    logger.LogInformation("Get forward message content:\n {Content}", forwardMsgContent);
#endif

                    sb.AppendLine(forwardMsgContent);
                    break;
#if DEBUG
                default:
                    logger.LogInformation("Reviced message not handlable.");
                    break;
#endif
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