using Makabaka.Messages;
using Microsoft.Extensions.Logging;
using TreePassBot.Data;
using TreePassBot.Data.Entities;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Services;

public class AudioService(
    IUserService userService,
    IMessageService messageService,
    JsonDataStore dataStore,
    ILogger<AudioService> logger) : IAuditService
{
    /// <inheritdoc />
    public async Task<bool> ProcessApprovalAsync(ulong targetQqId, ulong operatorQqId, ulong groupId)
    {
        var user = await userService.GetPendingUserAsync(targetQqId);
        if (user is not { Status: AuditStatus.Pending })
        {
            logger.LogError($"Targe user: {user} not contains in pending list.");

            await messageService.SendGroupMessageAsync(groupId,
                [new AtSegment(operatorQqId), new TextSegment("目标QQ号未找到！")]);

            return false;
        }

        var passcode = await GenerateUniquePasscodeAsync();

        // update data
        var success = await userService.UpdateUserStatusAsync(targetQqId, AuditStatus.Approved, passcode);

        if (success)
        {
            await messageService.SendPrivateMessageAsync(targetQqId,
                [new TextSegment($"您的审核已通过！请在入群申请中填写以下验证码：{passcode}")]);
        }

        logger.LogInformation($"User {targetQqId} has been approved by operator {operatorQqId}.");

        return true;
    }

    /// <inheritdoc />
    public async Task ProcessDenialAsync(ulong targetQqId, ulong operatorQqId)
    {
        await messageService.SendPrivateMessageAsync(targetQqId,
            [new TextSegment("很抱歉，您的审核未通过！")]);

        logger.LogInformation($"User {targetQqId} has been denied approval by operator {operatorQqId}.");
    }

    private Task<string> GenerateUniquePasscodeAsync()
    {
        // 循环生成直到找到一个唯一的
        while (true)
        {
            var passcode = new string(Enumerable.Range(0, 10)
                .Select(_ => (char)Random.Shared.Next('0', '9' + 1))
                .ToArray());

            if (!dataStore.PasscodeExists(passcode))
            {
                return Task.FromResult(passcode);
            }
        }
    }
}