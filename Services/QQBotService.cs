using Makabaka;
using Makabaka.Events;
using Microsoft.Extensions.Hosting;
using TreePassBot.Handlers;

namespace TreePassBot.Services;

public class QqBotService : IHostedService
{
    public static readonly MakabakaApp MakabakaApp = new MakabakaAppBuilder(Program.InArgs).Build();

    private static async Task Init(CancellationToken ct)
    {
        MakabakaApp.BotContext.OnGroupAddRequest += OnGroupAddRequestHelper;
        MakabakaApp.BotContext.OnGroupMessage += OnGroupMessageHelper;
        MakabakaApp.BotContext.OnGroupMemberIncrease += OnGroupMemberIncreaseHelper;
        MakabakaApp.BotContext.OnGroupMemberDecrease += OnGroupMemberDecreaseHelper;

        await MakabakaApp.StartAsync(ct);
    }

    private static Task OnGroupMemberDecreaseHelper(object sender, GroupMemberDecreaseEventArgs e)
    {
        var handler =
            (GroupMemberEventHandler)Program.AppHost.Services.GetService(typeof(GroupMemberEventHandler))!;
        return handler.HandleGroupMemberDecrease(e);
    }


    #region Helper Method

    private static Task OnGroupMemberIncreaseHelper(object sender, GroupMemberIncreaseEventArgs e)
    {
        var handler =
            (GroupMemberEventHandler)Program.AppHost.Services.GetService(typeof(GroupMemberEventHandler))!;
        return handler.HandleGroupMemberIncrease(e);
    }

    private static Task OnGroupAddRequestHelper(object sender, GroupAddRequestEventArgs e)
    {
        var handler = (GroupRequestEventHandler)Program.AppHost.Services.GetService(typeof(GroupRequestEventHandler))!;
        return handler.HandleAddRequest(e);
    }

    private static Task OnGroupMessageHelper(object sender, GroupMessageEventArgs e)
    {
        var handler =
            (GroupMessageEventHandler)Program.AppHost.Services.GetService(typeof(GroupMessageEventHandler))!;
        return handler.HandleGroupMessage(sender, e);
    }

    #endregion

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Init(cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await MakabakaApp.StopAsync(cancellationToken);
    }
}