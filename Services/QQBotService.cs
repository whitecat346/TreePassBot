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
        MakabakaApp.BotContext.OnGroupAddRequest += OnGroupAddRequest;
        MakabakaApp.BotContext.OnGroupMessage += OnGroupMessageHandlerMethod;

        await MakabakaApp.StartAsync(ct);
    }


    #region Helper Method

    private static Task OnGroupAddRequest(object sender, GroupAddRequestEventArgs e)
    {
        var handler = (GroupRequestEventHandler)Program.AppHost.Services.GetService(typeof(GroupRequestEventHandler))!;
        return handler.HandleAddRequest(sender, e);
    }

    private static Task OnGroupMessageHandlerMethod(object sender, GroupMessageEventArgs e)
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