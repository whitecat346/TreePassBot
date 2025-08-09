using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Makabaka.Events;
using Microsoft.Extensions.Options;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Handlers;

public class GroupRequestEventHandler(
    IUserService userService,
    IMessageService messageService,
    IOptions<BotConfig> config)
{
    public async void HandleAddRequest(object sender, GroupAddRequestEventArgs e)
    {
        if (e.GroupId != config.Value.AuditGroupId)
        {
            return;
        }
    }
}