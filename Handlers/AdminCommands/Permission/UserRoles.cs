namespace TreePassBot.Handlers.AdminCommands.Permission;

[Flags]
public enum UserRoles
{
    None = 0,
    GroupAdmin = 1 << 1,
    BotAdmin = 1 << 2,
    Auditor = 1 << 3
}