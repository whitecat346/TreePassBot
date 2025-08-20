using System.Reflection;
using TreePassBot.Handlers.AdminCommands.Permission;

namespace TreePassBot.Handlers.AdminCommands.Data;

public class CommandInfo(MethodInfo method, Type moduleType, BotCommand attribute, UserRoles roles)
{
    public MethodInfo Method { get; } = method;
    public Type ModuleType { get; } = moduleType;
    public BotCommand Attribute { get; } = attribute;

    public UserRoles Roles { get; } = roles;
}