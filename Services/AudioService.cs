using Makabaka.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Data.Entities;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;
using TreePassBot.Utils;

namespace TreePassBot.Services;

public class AudioService(
    IUserService userService,
    IMessageService messageService,
    PasscodeGeneratorUtil generator,
    IOptions<BotConfig> config,
    ILogger<AudioService> logger) : IAuditService
{
    /// <inheritdoc />
    public async Task<bool> ProcessApprovalAsync(ulong targetQqId, ulong operatorQqId, ulong groupId)
    {
        var user = await userService.GetPendingUserAsync(targetQqId);
        if (user is null)
        {
            logger.LogError($"Targe user: {targetQqId} not contains in pending list.");

            await messageService.SendGroupMessageAsync(groupId,
                [new AtSegment(operatorQqId), new TextSegment("目标QQ号未找到！")]);

            return false;
        }

        if (user.Status is AuditStatus.Approved or AuditStatus.Expried)
        {
            logger.LogError($"Targe user: {targetQqId} has been processed.");

            await messageService.SendGroupMessageAsync(groupId,
                [new AtSegment(operatorQqId), new TextSegment("目标QQ号已被处理！")]);

            return false;
        }

        var passcode = await generator.GenerateUniquePasscodeAsync();

        // update data
        var success = await userService.UpdateUserStatusAsync(targetQqId, AuditStatus.Approved, passcode);

        if (success)
        {
            await messageService.SendPrivateMessageAsync(targetQqId,
                [new TextSegment($"您的审核已通过！请在入群申请中填写以下验证码：{passcode}")]);
        }

        logger.LogInformation("User {TargetQqId} has been approved by operator {OperatorQqId}.", targetQqId, operatorQqId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessDenialAsync(ulong targetQqId, ulong operatorQqId, ulong groupId)
    {
        var user = await userService.GetPendingUserAsync(targetQqId);
        if (user is null)
        {
            logger.LogError("Targe user: {TargetQqId} not contains in pending list.", targetQqId);

            await messageService.SendGroupMessageAsync(groupId,
                [new AtSegment(operatorQqId), new TextSegment("目标QQ号未找到！")]);

            return false;
        }

        if (user.Status is AuditStatus.Approved or AuditStatus.Expried)
        {
            logger.LogError("Targe user: {TargetQqId} has been processed.", targetQqId);

            await messageService.SendGroupMessageAsync(groupId,
                [new AtSegment(operatorQqId), new TextSegment("目标QQ号已被处理！")]);

            return false;
        }

        string reason;
        AuditStatus status;
        switch (user.Status )
            {
                case AuditStatus.Pending :
                    status = AuditStatus.Suspend;
                    reason = "您有3次审核机会";
                    break;
                case AuditStatus.Suspend:
                    status = AuditStatus.Dying;
                    reason = "您还有2次审核机会";
                    break;
                case AuditStatus.Dying:
                    status = AuditStatus.Denied;
                    reason = "您还有1次审核机会";
                    break;
                default:
                    reason = "很抱歉，您的三次审核机会已用尽，无法再申请审核！";
                    status = AuditStatus.Denied;
                    break;
            }

        if (status is AuditStatus.Denied)
        {
            await messageService.SendPrivateMessageAsync(targetQqId, [new TextSegment(reason)]);
            await QqBotService.MakabakaApp.BotContext.KickGroupMemberAsync(config.Value.AuditGroupId, targetQqId);

            // unsure whether kick cause GroupMemberDrcrease event
            // and which will cause delete user event in this program.
            // so i decided to call delete user directly
            // for ensure user is deleted from data store.
            await userService.DeleteUserAsync(targetQqId);

            return true;
        }

        await userService.UpdateUserStatusAsync(targetQqId, status, string.Empty);

        await messageService.SendPrivateMessageAsync(targetQqId,
            [new TextSegment("很抱歉，您的审核未通过！"), new TextSegment(reason)]);

        logger.LogInformation("User {TargetQqId} has been denied approval by operator {OperatorQqId}.", targetQqId, operatorQqId);

        return true;
    }
}