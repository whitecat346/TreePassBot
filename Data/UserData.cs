using TreePassBot.Data.Entities;

namespace TreePassBot.Data;

public record UserData
{
    public List<UserInfo> Users { get; init; } = [];

    public List<ulong> BlackList { get; init; } = [];
}