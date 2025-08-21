using System.Text;
using Makabaka.Events;
using Makabaka.Messages;
using TreePassBot.Handlers.AdminCommands.Data;
using TreePassBot.Handlers.AdminCommands.Permission;

namespace TreePassBot.Handlers.AdminCommands.Commands;

public class HelpCommands(CommandDispatcher dispatcher)
{
    [BotCommand("help", Description = "显示所有的帮助信息", Usage = ".help")]
    [RequiredPremission(UserRoles.Auditor | UserRoles.BotAdmin | UserRoles.GroupAdmin)]
    public async Task<bool> ShowHelp(GroupMessageEventArgs e)
    {
        var helpMessage = new StringBuilder("可用的命令列表:\n");
        foreach (var command in dispatcher.Commands.Values)
        {
            helpMessage.AppendLine($"- {command.Attribute.Name}: {command.Attribute.Description}");
            if (!string.IsNullOrEmpty(command.Attribute.Usage))
            {
                helpMessage.AppendLine($"  用法: {command.Attribute.Usage}");
            }
        }

        await e.ReplyAsync([new AtSegment(e.UserId), new TextSegment(helpMessage.ToString())]).ConfigureAwait(false);
        return true;
    }
}