using Makabaka.Events;
using Makabaka.Messages;
using TreePassBot.Data;
using TreePassBot.Handlers.Commands.Data;
using TreePassBot.Handlers.Commands.Permission;

namespace TreePassBot.Handlers.Commands.Commands;

public class Tests(JsonDataStore dataStore)
{
    [BotCommand("rand", Description = "生成一个随机验证码", Usage = ".rand")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> RandomGeneration(GroupMessageEventArgs e)
    {
        var passcode = new string([.. Enumerable.Range(0, 10).Select(_ => (char)Random.Shared.Next('0', '9' + 1))]);

        var isUnique = !dataStore.PasscodeExists(passcode);

        await e.ReplyAsync([new TextSegment($"生成测试验证码: {passcode}\n在当前数据中是否唯一: {isUnique}")]).ConfigureAwait(false);

        return true;
    }
}