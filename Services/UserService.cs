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
    public Task<bool> UpdateUserStatusAsync(ulong qqId, AuditStatus status, string? passcode = null)
    {
        var user = dataStore.GetUserByQqId(qqId);
        if (user == null) return Task.FromResult(false);

        // 如果生成了验证码，需要确保验证码是唯一的
        if (passcode != null && dataStore.PasscodeExists(passcode))
        {
            logger.LogError("Generated passcode {Passcode} already exists. This should be rare.", passcode);
            // 这里可以加入重试逻辑
            return Task.FromResult(false);
        }

        user.Status = status;
        user.Passcode = passcode ?? string.Empty;
        user.UpdatedAt = DateTime.UtcNow;

        dataStore.UpdateUser(user);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> ValidateJoinRequestAsync(ulong qqId, string passcode)
    {
        var user = dataStore.GetUserByQqId(qqId);
        if (user == null)
        {
            return Task.FromResult(false);
        }

        if (user.Passcode.Equals(passcode, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}