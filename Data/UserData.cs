using TreePassBot.Data.Entities;

namespace TreePassBot.Data;

public record UserData
{
    public List<PendingUser> Users { get; set; } = [];

    public List<ulong> BlackList { get; set; } = [];
}