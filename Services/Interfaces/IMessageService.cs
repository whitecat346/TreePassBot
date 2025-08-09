namespace TreePassBot.Services.Interfaces;

public interface IMessageService
{
    public Task SendGroupMessageAsync(ulong groupId, Makabaka.Messages.Message msg);
    public Task SendPrivateMessageAsync(ulong userId, Makabaka.Messages.Message msg);
}