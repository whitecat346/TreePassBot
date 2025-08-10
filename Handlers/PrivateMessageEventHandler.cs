using Makabaka.Events;
using Makabaka.Messages;
using Microsoft.Extensions.Logging;
using TreePassBot.Data;
using TreePassBot.Data.Entities;
using TreePassBot.Utils;

namespace TreePassBot.Handlers;

public class PrivateMessageEventHandler(
    JsonDataStore dataStore,
    PasscodeGeneratorUtil generator,
    ILogger<PrivateMessageEventHandler> logger)
{
    public async Task HandlePrivateMessage(PrivateMessageEventArgs e)
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

            await e.ReplyAsync([new TextSegment("您的新验证码是："), new TextSegment(user.Passcode)]);

            logger.LogInformation("User {qqId} passed expried, generating new passcode: {passcode}.", e.UserId, passcode);
        }
    }
}