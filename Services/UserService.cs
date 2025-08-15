using Microsoft.Extensions.Logging;
using TreePassBot.Data;
using TreePassBot.Data.Entities;
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

        var newUser = new PendingUser
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
    public Task<PendingUser?> GetPendingUserAsync(ulong qqId)
    {
        return Task.FromResult(dataStore.GetUserByQqId(qqId));
    }

    /// <inheritdoc />
    public Task<bool> UpdateUserStatusAsync(ulong qqId, AuditStatus status, string passcode)
    {
        var user = dataStore.GetUserByQqId(qqId);
        if (user == null) return Task.FromResult(false);

        user.Status = status;
        user.Passcode = passcode;
        user.UpdatedAt = DateTime.UtcNow;

        if (status is AuditStatus.Approved or AuditStatus.Expried)
        {
            user.ExpriedAt = DateTime.UtcNow + TimeSpan.FromMinutes(10);
        }

        dataStore.UpdateUser(user);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<(bool, bool)> ValidateJoinRequestAsync(ulong qqId, string passcode)
    {
        var user = dataStore.GetUserByQqId(qqId);
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User not found in data store.");
        }

        var rightPasscode = user.Passcode.Equals(passcode, StringComparison.OrdinalIgnoreCase);
        var expriedPassscode = user.Status == AuditStatus.Expried;

        return Task.FromResult((rightPasscode, expriedPassscode));
    }

    /// <inheritdoc />
    public Task DeleteUserAsync(ulong qqId)
    {
        dataStore.DeleteUser(qqId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddToBlackList(ulong qqId)
    {
        dataStore.AddToBlackList(qqId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> IsInBlackList(ulong qqId)
    {
        var isBlack = dataStore.IsInBlackList(qqId);
        return Task.FromResult(isBlack);
    }

    /// <inheritdoc />
    public Task RemoveFromBlackList(ulong qqId)
    {
        dataStore.RemoveFromBlackList(qqId);
        return Task.CompletedTask;
    }
}