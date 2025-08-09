using TreePassBot.Data.Entities;

namespace TreePassBot.Data;

public record PendingUserData
{
    public List<PendingUser> Users { get; set; } = [];
}