using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public async Task ProcessApprovalAsync(ulong targetQqId, ulong operatorQqId)
    {
        var user = await userService.GetPendingUserAsync(targetQqId);
        if (user is not { Status: AuditStatus.Pending })
        {
            logger.LogError($"Targe user: {user} not contains in pending list.");
            return;
        }

        var passcode = await GenerateUniquePasscodeAsync();
        
        // update data
        var success = await userService.UpdateUserStatusAsync(targetQqId, AuditStatus.Approved, passcode);

        if (success)
        {
            // TODO: impl message service
                targetQqId,
                $"您的审核已通过！请使用以下验证码登录：{passcode}");
        }
    }

    /// <inheritdoc />
    public Task ProcessDenialAsync(ulong targetQqId, ulong operatorQqId)
    {
        return null;
    }

    private async Task<string> GenerateUniquePasscodeAsync()
    {
        // 循环生成直到找到一个唯一的
        while (true)
        {
            var passcode = new string(Enumerable.Range(0, 10)
                .Select(_ => (char)Random.Shared.Next('0', '9' + 1))
                .ToArray());

            if (!dataStore.PasscodeExists(passcode))
            {
                return passcode;
            }
        }
    }

    /// <inheritdoc />
    public Task ProcessDenialAsync(long targetQqId, ulong operatorQqId)
    {
        return null;
    }
}