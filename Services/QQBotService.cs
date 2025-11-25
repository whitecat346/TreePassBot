using Makabaka;
using Makabaka.Events;
using Microsoft.Extensions.Hosting;
using TreePassBot.Handlers;

// ReSharper disable InconsistentNaming

namespace TreePassBot.Services;

public class QqBotService : IHostedService
{
    public static readonly MakabakaApp MakabakaApp = new MakabakaAppBuilder(Program.InArgs).Build();

    private static Task Init(CancellationToken ct)
    {
        MakabakaApp.BotContext.OnGroupAddRequest += OnGroupAddRequestHelper;
        MakabakaApp.BotContext.OnGroupMessage += OnGroupMessageHelper;
        MakabakaApp.BotContext.OnGroupMemberIncrease += OnGroupMemberIncreaseHelper;
        MakabakaApp.BotContext.OnGroupMemberDecrease += OnGroupMemberDecreaseHelper;
        MakabakaApp.BotContext.OnPrivateMessage += OnPrivateMessageHelper;

        return MakabakaApp.StartAsync(ct);
    }

    #region Helper Method

    private static readonly PrivateMessageEventHandler _privateMessageEventHandler =
        (PrivateMessageEventHandler)Program.AppHost.Services.GetService(typeof(PrivateMessageEventHandler))!;

    private static readonly GroupMemberEventHandler _groupMemberEventHandler =
        (GroupMemberEventHandler)Program.AppHost.Services.GetService(typeof(GroupMemberEventHandler))!;

    private static readonly GroupRequestEventHandler _groupRequestEventHandler =
        (GroupRequestEventHandler)Program.AppHost.Services.GetService(typeof(GroupRequestEventHandler))!;

    private static readonly GroupMessageEventHandler _groupMessageEventHandler =
        (GroupMessageEventHandler)Program.AppHost.Services.GetService(typeof(GroupMessageEventHandler))!;

    private static Task OnPrivateMessageHelper(object sender, PrivateMessageEventArgs e)
    {
        return _privateMessageEventHandler.HandlePrivateMessage(e);
    }

    private static Task OnGroupMemberIncreaseHelper(object sender, GroupMemberIncreaseEventArgs e)

    {
        return _groupMemberEventHandler.HandleGroupMemberIncrease(e);
    }

    private static Task OnGroupMemberDecreaseHelper(object sender, GroupMemberDecreaseEventArgs e)
    {
        return _groupMemberEventHandler.HandleGroupMemberDecrease(e);
    }

    private static Task OnGroupAddRequestHelper(object sender, GroupAddRequestEventArgs e)
    {
        return _groupRequestEventHandler.HandleAddRequest(e);
    }

    private static Task OnGroupMessageHelper(object sender, GroupMessageEventArgs e)
    {
        return _groupMessageEventHandler.HandleGroupMessageAsync(e);
    }

    #endregion

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Init(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return MakabakaApp.StopAsync(cancellationToken);
    }
}