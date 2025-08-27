namespace TreePassBot.Handlers.Commands.Permission;

[Flags]
public enum UserRoles
{
    None = 1 << 1,
    GroupAdmin = 1 << 2,
    BotAdmin = 1 << 3,
    Auditor = 1 << 4
}