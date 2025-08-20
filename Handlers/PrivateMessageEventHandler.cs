using Makabaka.Events;
using Makabaka.Messages;
using Microsoft.Extensions.Logging;

namespace TreePassBot.Handlers;

public class PrivateMessageEventHandler(ILogger<PrivateMessageEventHandler> logger)
{
    public async Task HandlePrivateMessage(PrivateMessageEventArgs e)
    {
        logger.LogInformation("New private message: '{Msg}' - from: {Id}", e.Message.ToString(), e.UserId);
        await e.ReplyAsync([new TextSegment("此账户为机器人账户，反馈问题请咨询其他管理。")]);
    }
}