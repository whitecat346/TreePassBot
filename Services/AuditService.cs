using Makabaka.Messages;
using Microsoft.Extensions.Logging;
using TreePassBot.Data.Entities;
using TreePassBot.Services.Interfaces;
using TreePassBot.Utils;

namespace TreePassBot.Services;

public class AuditService(
    IUserService userService,
    IMessageService messageService,
    PasscodeGeneratorUtil generator,
    ILogger<AuditService> logger) : IAuditService
{
    /// <inheritdoc />
    public async Task<bool> ProcessApprovalAsync(ulong targetQqId, ulong operatorQqId, ulong groupId)
    {
        var user = await userService.GetUserInfoByIdAsync(targetQqId).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogError("Targe user: {TargetQqId} not contains in pending list.", targetQqId);

            await messageService.SendGroupMessageAsync(groupId,
            [
                new AtSegment(operatorQqId),
                new TextSegment("目标QQ号未找到！")
            ]).ConfigureAwait(false);

            return false;
        }

        if (user.Status is AuditStatus.Approved)
        {
            logger.LogError("Targe user: {TargetQqId} has been processed.", targetQqId);

            await messageService.SendGroupMessageAsync(groupId, [
                new AtSegment(operatorQqId),
                new TextSegment("目标QQ号已被处理！")
            ]).ConfigureAwait(false);

            return false;
        }

        var passcode = await generator.GenerateUniquePasscodeAsync();

        // update data
        var success =
            await userService.TryUpdateUserStatusAsync(targetQqId, AuditStatus.Approved, passcode, out _)
                             .ConfigureAwait(false);

        if (success)
        {
            logger.LogInformation("User {TargetQqId} has been approved by operator {OperatorQqId}.", targetQqId,
                                  operatorQqId);

            await messageService.SendGroupMessageAsync(groupId,
            [
                new AtSegment(targetQqId),
                new TextSegment($"您的审核已通过！请在入群申请中填写以下验证码：{passcode}\n"),
                new TextSegment("该验证码将在10分钟后过期，过期后就要重新答题了，所以尽快使用。\n"),
                new TextSegment("验证码与QQ号一一对应，不用再尝试其他人的验证码了 (～￣▽￣)～")
            ]).ConfigureAwait(false);
        }
        else
        {
            await messageService.SendGroupMessageAsync(groupId, [
                new AtSegment(operatorQqId),
                new TextSegment("在尝试变更用户信息时失败。")
            ]).ConfigureAwait(false);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessDenialAsync(ulong targetId, ulong operatorQqId, ulong groupId)
    {
        var user = await userService.GetUserInfoByIdAsync(targetId);
        if (user is null)
        {
            logger.LogError("Targe user: {TargetQqId} not contains in pending list.", targetId);

            await messageService.SendGroupMessageAsync(groupId,
                                                       [new AtSegment(operatorQqId), new TextSegment("目标QQ号未找到！")]);

            return false;
        }

        if (user.Status is AuditStatus.Approved or AuditStatus.Expired)
        {
            logger.LogError("Targe user: {TargetQqId} has been processed.", targetId);

            await messageService.SendGroupMessageAsync(groupId,
            [
                new AtSegment(operatorQqId),
                new TextSegment("目标QQ号已被处理！")
            ]).ConfigureAwait(false);

            return false;
        }

        string lastChance;
        AuditStatus status;
        switch (user.Status)
        {
            case AuditStatus.Pending:
                status = AuditStatus.Suspend;
                lastChance = "您还有2次审核机会";
                break;
            case AuditStatus.Suspend:
                status = AuditStatus.Dying;
                lastChance = "您还有1次审核机会";
                break;
            case AuditStatus.Dying:
                status = AuditStatus.Denied;
                lastChance = "很抱歉，您的三次审核机会已用尽！\n因为一些未知的问题无法自动踢出群聊，请自行退出，否则待审核名单里没你的信息。";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(user.Status));
        }

        if (status is AuditStatus.Denied)
        {
            await messageService.SendGroupMessageAsync(groupId,
                                                       [new AtSegment(targetId), new TextSegment(lastChance)])
                                .ConfigureAwait(false);

            await messageService.KickGroupMemberAsync(groupId, targetId).ConfigureAwait(false);

            // unsure whether kick cause GroupMemberDecrease event
            // and which will cause delete user event in this program.
            // so i decided to call delete user directly
            // for ensure user is deleted from data store.
            await userService.DeleteUserAsync(targetId).ConfigureAwait(false);

            logger.LogInformation("User {TargetQqId} has been kicked and deleted from data store.", targetId);

            return true;
        }

        await userService.TryUpdateUserStatusAsync(targetId, status, string.Empty, out _).ConfigureAwait(false);

        await messageService.SendGroupMessageAsync(groupId,
        [
            new AtSegment(targetId),
            new TextSegment("很抱歉，您的审核未通过！"),
            new TextSegment(lastChance)
        ]).ConfigureAwait(false);

        logger.LogInformation("User {TargetQqId} has been denied approval by operator {OperatorQqId}.", targetId,
                              operatorQqId);

        return true;
    }
}