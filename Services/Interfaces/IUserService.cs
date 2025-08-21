using TreePassBot.Data.Entities;

namespace TreePassBot.Services.Interfaces;

public interface IUserService
{
    Task<bool> AddPendingUserAsync(ulong qqId);
    Task<UserInfo?> GetPendingUserAsync(ulong qqId);
    Task<bool> TryUpdateUserStatusAsync(ulong qqId, AuditStatus status, string passcode, out UserInfo? updatedUser);
    Task<(bool, bool)> ValidateJoinRequestAsync(ulong qqId, string passcode);
    Task DeleteUserAsync(ulong qqId);

    Task AddToBlackList(ulong qqId);
    Task<bool> IsInBlackList(ulong qqId);

    Task RemoveFromBlackList(ulong qqId);
}