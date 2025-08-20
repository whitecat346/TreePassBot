using System.Reflection;
using Makabaka.Events;
using Makabaka.Messages;
using Makabaka.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Handlers.AdminCommands.Permission;
using TreePassBot.Models;

namespace TreePassBot.Handlers.AdminCommands;

public class CommandDispatcher
{
    private readonly BotConfig _config;
    private readonly char _prefix;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandDispatcher> _logger;
    private readonly Dictionary<string, CommandInfo> _commands = new();

    public IReadOnlyDictionary<string, CommandInfo> Commands => _commands;

    public CommandDispatcher(
        IServiceProvider serviceProvider,
        ILogger<CommandDispatcher> logger,
        IOptions<BotConfig> config,
        char prefix = '.')
    {
        _config = config.Value;
        _prefix = prefix;
        _serviceProvider = serviceProvider;
        _logger = logger;

        RegisterCommands(Assembly.GetExecutingAssembly());
    }

    private void RegisterCommands(Assembly assembly)
    {
        var commandModulesTypes = assembly.GetTypes()
                                          .Where(t => t is { IsClass: true, IsAbstract: false } &&
                                                      t.GetMethods().Any(m => m.IsDefined(typeof(BotCommand))));

        foreach (var moduleType in commandModulesTypes)
        {
            var methods = moduleType.GetMethods()
                                    .Where(m => m.IsDefined(typeof(BotCommand)));

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<BotCommand>();
                if (attr == null)
                {
                    continue;
                }

                var rolesAttr = method.GetCustomAttribute<RequiredPremission>();
                var roles = rolesAttr?.RequiredRoles ?? UserRoles.None;

                var parameters = method.GetParameters();
                if (parameters.Length != 1
                 || parameters[0].ParameterType != typeof(GroupMessageEventArgs))
                {
                    _logger.LogWarning("Command {Name} has invalid parameters, skipped", method.Name);

                    continue;
                }

                var commandInfo = new CommandInfo(method, moduleType, attr, roles);

                if (!_commands.TryAdd(attr.Name, commandInfo))
                {
                    _logger.LogWarning("Command {Name} duplicated, skipped.", attr.Name);


                    continue;
                }

                foreach (var alias in attr.Aliases)
                {
                    if (!_commands.TryAdd(alias.ToLowerInvariant(), commandInfo))
                    {
                        _logger.LogWarning("Command alias {Name} duplicated, skipped.", alias);
                    }
                }
            }
        }

        _logger.LogInformation("Successed to register {Count} commands.", _commands.Count);
    }

    public async Task ExecteAsync(GroupMessageEventArgs e)
    {
        var message = e.Message.ToString().Trim();

        if (string.IsNullOrWhiteSpace(message) || message[0] != _prefix)
        {
            return;
        }

        var parts = message[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var commandName = parts[0].ToLowerInvariant();

        if (!_commands.TryGetValue(commandName, out var commandInfo))
        {
            await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment("未知命令："), new TextSegment(commandName)]);
            _logger.LogWarning("Command {Command} not found.", commandName);

            return;
        }

        _logger.LogInformation("User {ID} try to issue command {Name}.", e.UserId, commandName);

        if (!IsExecutable(e, commandInfo.Roles))
        {
            _logger.LogInformation("Un-executable command {Name} issued.", commandName);
            return;
        }

        _logger.LogInformation("Execute command {Name} by {UserId}.", commandName, e.UserId);

        using var scoop = _serviceProvider.CreateScope();
        try
        {
            var moduleInstance = scoop.ServiceProvider.GetRequiredService(commandInfo.ModuleType);

            var task = (Task<bool>?)commandInfo.Method.Invoke(moduleInstance, [e]);
            if (task != null)
            {
                var result = await task;

                if (result == false)
                {
                    await e.ReplyAsync([
                        new AtSegment(e.UserId), new TextSegment("命令执行失败\n使用方法："),
                        new TextSegment(commandInfo.Attribute.Usage)
                    ]);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to execute command {Name} {Message}",
                             commandName,
                             ex.InnerException?.Message ?? ex.Message);
        }
    }

    private bool IsExecutable(GroupMessageEventArgs e, UserRoles role)
    {
        if (role.HasFlag(UserRoles.None))
        {
            return true;
        }

        var isGroupAdmin = false;
        var isBotAdmin = false;
        var isAuditor = false;

        if (role.HasFlag(UserRoles.GroupAdmin))
        {
            isGroupAdmin = e.Sender?.Role is GroupRoleType.Admin or GroupRoleType.Owner;
        }

        if (role.HasFlag(UserRoles.BotAdmin))
        {
            isBotAdmin = _config.AdminQqIds.Contains(e.UserId);
        }

        if (role.HasFlag(UserRoles.Auditor))
        {
            isAuditor = _config.AuditorQqIds.Contains(e.UserId);
        }

        return isGroupAdmin || isBotAdmin || isAuditor;
    }
}