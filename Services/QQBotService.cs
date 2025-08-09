using Makabaka;
using Makabaka.Events;
using Microsoft.Extensions.Hosting;
using TreePassBot.Handlers;

namespace TreePassBot.Services;

public class QQBotService : IHostedService
{
    private static readonly MakabakaApp _app = new MakabakaAppBuilder().Build();

    public static async Task Init(CancellationToken ct)
    {
        _app.BotContext.OnGroupAddRequest += OnGroupAddRequest;

        await _app.StartAsync(ct);
    }


    #region Helper Method

    private static Task OnGroupAddRequest(object sender, GroupAddRequestEventArgs e)
    {
        var handler = (GroupRequestEventHandler)Program.AppHost.Services.GetService(typeof(GroupRequestEventHandler))!;
        handler.HandleAddRequest(sender, e);
        return Task.CompletedTask;
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
        await _app.StopAsync(cancellationToken);
    }
}