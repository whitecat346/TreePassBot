using Makabaka.Models;

namespace TreePassBot.Services.Interfaces;

public interface IMessageService
{
    Task SendGroupMessageAsync(ulong groupId, Makabaka.Messages.Message msg);

    Task SendPrivateMessageAsync(ulong userId, Makabaka.Messages.Message msg);

    Task<GroupMemberInfo?> GetGroupMemberInfo(ulong groupId, ulong userId);

    Task<GroupMemberInfo[]?> GetGroupMemberList(ulong groupId);

    Task KickGroupMemberAsync(ulong groupId, ulong userId);

    Task DeleteMessageAsync(long messageId);

    Task<ForwardMessageInfo?> GetForwardMessageAsync(string forwardId);
}