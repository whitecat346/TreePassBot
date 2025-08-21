using Microsoft.Extensions.Logging;
using TreePassBot.Data;
using TreePassBot.Data.Entities;
using TreePassBot.Exceptions;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Services;

public class UserService(
    JsonDataStore dataStore,
    ILogger<UserService> logger) : IUserService
{
    /// <inheritdoc />
    public Task<bool> AddPendingUserAsync(ulong qqId)
    {
        if (dataStore.UserExists(qqId))
        {
            logger.LogInformation("User {QqId} already in pending list, skipping.", qqId);
            return Task.FromResult(true); // 认为是成功，因为目标状态（用户在列表里）已达成
        }

        var newUser = new UserInfo
        {
            QqId = qqId,
            Status = AuditStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dataStore.AddUser(newUser);
        logger.LogInformation("User {QqId} added to pending list.", qqId);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<UserInfo?> GetUserInfoByIdAsync(ulong qqId)
    {
        logger.LogInformation("Try to get user {QqId}.", qqId);
        return Task.FromResult(dataStore.GetUserByQqId(qqId));
    }

    /// <inheritdoc />
    public Task<bool> TryUpdateUserStatusAsync(
        ulong qqId, AuditStatus status, string passcode, out UserInfo? updatedUser)
    {
        var user = dataStore.GetUserByQqId(qqId);
        if (user == null)
        {
            updatedUser = null;
            return Task.FromResult(false);
        }

        user.Status = status;
        user.Passcode = passcode;
        user.UpdatedAt = DateTime.UtcNow;

        if (status is AuditStatus.Approved or AuditStatus.Expired)
        {
            user.ExpriedAt = DateTime.UtcNow + TimeSpan.FromMinutes(10);
        }

        dataStore.UpdateUser(user);

        logger.LogInformation("Update user {QqId} status to {State}.", qqId, status);

        updatedUser = user;
        return Task.FromResult(true);
    }

    /// <exception cref="UserNotFoundException">用户未找到</exception>
    /// <inheritdoc />
    public Task<(bool, bool)> ValidateJoinRequestAsync(ulong qqId, string passcode)
    {
        var user = dataStore.GetUserByQqId(qqId);
        if (user == null)
        {
            throw new UserNotFoundException(qqId);
        }

        logger.LogInformation("Validate user {QqId}.", qqId);
        var rightPasscode = user.Passcode.Equals(passcode, StringComparison.OrdinalIgnoreCase);
        var expriedPassscode = user.Status == AuditStatus.Expired;

        return Task.FromResult((rightPasscode, expriedPassscode));
    }

    /// <inheritdoc />
    public Task DeleteUserAsync(ulong qqId)
    {
        dataStore.DeleteUser(qqId);
        logger.LogInformation("Delete user {QqId} from audit list.", qqId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddToBlackList(ulong qqId)
    {
        dataStore.AddToBlackList(qqId);
        logger.LogInformation("Add user {QqId} to black list.", qqId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> IsInBlackList(ulong qqId)
    {
        var isBlack = dataStore.IsInBlackList(qqId);
        logger.LogInformation("Check user {QqId} whether in black list.", qqId);
        return Task.FromResult(isBlack);
    }

    /// <inheritdoc />
    public Task RemoveFromBlackList(ulong qqId)
    {
        dataStore.RemoveFromBlackList(qqId);
        logger.LogInformation("Remove user {QqId} from black list.", qqId);
        return Task.CompletedTask;
    }
}