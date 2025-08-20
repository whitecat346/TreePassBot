namespace TreePassBot.Handlers.AdminCommands.Data;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class BotCommand(string name) : Attribute
{
    /// <summary>
    /// Command name.
    /// </summary>
    public string Name { get; } = name.ToLowerInvariant();

    /// <summary>
    /// Command description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// How to use the command.
    /// Used when user failed to use the command.
    /// </summary>
    public string Usage { get; set; } = string.Empty;

    /// <summary>
    /// Command aliases list.
    /// </summary>
    public string[] Aliases { get; set; } = [];
}