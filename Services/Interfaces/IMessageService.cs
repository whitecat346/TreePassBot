using Makabaka.Models;

namespace TreePassBot.Services.Interfaces;

public interface IMessageService
{
    Task SendGroupMessageAsync(ulong groupId, Makabaka.Messages.Message msg);

    [Obsolete("Cannot create temp room, so obsolete this method.")]
    Task SendPrivateMessageAsync(ulong userId, Makabaka.Messages.Message msg);

    Task<GroupMemberInfo?> GetGroupMemberInfo(ulong groupId, ulong userId);

    Task<GroupMemberInfo[]?> GetGroupMemberList(ulong groupId);
}